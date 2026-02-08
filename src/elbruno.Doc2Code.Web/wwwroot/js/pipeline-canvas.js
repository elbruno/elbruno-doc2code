// elbruno.Doc2Code — JS interop for the pipeline designer canvas (node drag, SVG edges, layout).

window.pipelineCanvas = (function () {
    "use strict";

    let _dotNetRef = null;
    let _containerId = "";
    let _nodes = {};
    let _svgEl = null;
    let _dragState = null;

    function _px(v) { return v + "px"; }

    function _buildNodeEl(step, agentName) {
        const el = document.createElement("div");
        el.className = "pd-node";
        el.dataset.stepId = step.stepId;
        el.style.left = _px(step.positionX);
        el.style.top = _px(step.positionY);
        el.innerHTML =
            '<span class="pd-node-label">' + _escHtml(agentName || step.agentKey) + '</span>' +
            (step.retryPolicy ? '<span class="pd-retry-badge" title="Retry policy">&#x21bb; ' + step.retryPolicy.maxRetries + '</span>' : '');

        el.addEventListener("pointerdown", function (ev) { _startDrag(ev, step.stepId); });
        el.addEventListener("click", function (ev) {
            ev.stopPropagation();
            _selectNode(step.stepId);
        });
        return el;
    }

    function _escHtml(s) {
        var d = document.createElement("span");
        d.textContent = s;
        return d.innerHTML;
    }

    function _selectNode(stepId) {
        var container = document.getElementById(_containerId);
        if (!container) return;
        container.querySelectorAll(".pd-node").forEach(function (n) {
            n.classList.toggle("pd-node-selected", n.dataset.stepId === stepId);
        });
        if (_dotNetRef) _dotNetRef.invokeMethodAsync("OnNodeSelectedJs", stepId);
    }

    function _startDrag(ev, stepId) {
        ev.preventDefault();
        var nodeEl = _nodes[stepId];
        if (!nodeEl) return;
        var rect = nodeEl.getBoundingClientRect();
        _dragState = {
            stepId: stepId,
            offsetX: ev.clientX - rect.left,
            offsetY: ev.clientY - rect.top
        };
        document.addEventListener("pointermove", _onDragMove);
        document.addEventListener("pointerup", _onDragEnd);
    }

    function _onDragMove(ev) {
        if (!_dragState) return;
        var container = document.getElementById(_containerId);
        if (!container) return;
        var cRect = container.getBoundingClientRect();
        var newX = ev.clientX - cRect.left - _dragState.offsetX;
        var newY = ev.clientY - cRect.top - _dragState.offsetY;
        newX = Math.max(0, newX);
        newY = Math.max(0, newY);
        var nodeEl = _nodes[_dragState.stepId];
        if (nodeEl) {
            nodeEl.style.left = _px(newX);
            nodeEl.style.top = _px(newY);
        }
        _redrawEdges();
    }

    function _onDragEnd() {
        if (_dragState) {
            var nodeEl = _nodes[_dragState.stepId];
            if (nodeEl && _dotNetRef) {
                var finalX = parseFloat(nodeEl.style.left) || 0;
                var finalY = parseFloat(nodeEl.style.top) || 0;
                _dotNetRef.invokeMethodAsync("OnNodeMovedJs", _dragState.stepId, finalX, finalY);
            }
        }
        _dragState = null;
        document.removeEventListener("pointermove", _onDragMove);
        document.removeEventListener("pointerup", _onDragEnd);
    }

    function _nodeCenterCoords(stepId) {
        var el = _nodes[stepId];
        if (!el) return null;
        var x = parseFloat(el.style.left) || 0;
        var y = parseFloat(el.style.top) || 0;
        var w = el.offsetWidth || 140;
        var h = el.offsetHeight || 48;
        return { cx: x + w / 2, cy: y + h / 2, right: x + w, left: x, top: y, bottom: y + h };
    }

    function _redrawEdges() {
        if (!_svgEl) return;
        while (_svgEl.firstChild) _svgEl.removeChild(_svgEl.firstChild);
        var container = document.getElementById(_containerId);
        if (!container || !container._edges) return;
        container._edges.forEach(function (edge) {
            var src = _nodeCenterCoords(edge.sourceStepId);
            var tgt = _nodeCenterCoords(edge.targetStepId);
            if (!src || !tgt) return;

            var startX = src.right;
            var startY = src.cy;
            var endX = tgt.left;
            var endY = tgt.cy;
            var midX = (startX + endX) / 2;

            var pathData = "M " + startX + " " + startY +
                " C " + midX + " " + startY + ", " + midX + " " + endY + ", " + endX + " " + endY;

            var pathEl = document.createElementNS("http://www.w3.org/2000/svg", "path");
            pathEl.setAttribute("d", pathData);
            pathEl.setAttribute("class", "pd-edge-path");
            _svgEl.appendChild(pathEl);

            // arrowhead triangle at the end
            var arrowLen = 8;
            var dx = endX - midX;
            var dy = endY - endY; // horizontal approach
            var angle = Math.atan2(endY - startY, endX - startX);
            if (Math.abs(endX - startX) > 10) angle = 0; // mostly horizontal
            var ax1 = endX - arrowLen * Math.cos(angle - 0.4);
            var ay1 = endY - arrowLen * Math.sin(angle - 0.4);
            var ax2 = endX - arrowLen * Math.cos(angle + 0.4);
            var ay2 = endY - arrowLen * Math.sin(angle + 0.4);
            var arrowPath = "M " + endX + " " + endY + " L " + ax1 + " " + ay1 + " L " + ax2 + " " + ay2 + " Z";
            var arrowEl = document.createElementNS("http://www.w3.org/2000/svg", "path");
            arrowEl.setAttribute("d", arrowPath);
            arrowEl.setAttribute("class", "pd-edge-arrow");
            _svgEl.appendChild(arrowEl);
        });
    }

    // ── Public API ─────────────────────────────────────────────────

    function initCanvas(dotNetRef, containerId, pipelineJson) {
        _dotNetRef = dotNetRef;
        _containerId = containerId;
        _nodes = {};

        var container = document.getElementById(containerId);
        if (!container) return;
        // Clear previous content
        container.innerHTML = "";

        var pipeline = typeof pipelineJson === "string" ? JSON.parse(pipelineJson) : pipelineJson;

        // Create SVG overlay for edges
        _svgEl = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        _svgEl.setAttribute("class", "pd-svg-overlay");
        container.appendChild(_svgEl);

        // Store edges on container for later redraws
        container._edges = pipeline.edges || [];

        // Render step nodes
        (pipeline.steps || []).forEach(function (step) {
            var agentDisplayName = step.agentKey;
            if (pipeline._agentNames && pipeline._agentNames[step.agentKey]) {
                agentDisplayName = pipeline._agentNames[step.agentKey];
            }
            var nodeEl = _buildNodeEl(step, agentDisplayName);
            container.appendChild(nodeEl);
            _nodes[step.stepId] = nodeEl;
        });

        // Draw initial edges
        requestAnimationFrame(function () { _redrawEdges(); });
    }

    function updateLayout(pipelineJson) {
        var container = document.getElementById(_containerId);
        if (!container) return;
        initCanvas(_dotNetRef, _containerId, pipelineJson);
    }

    function autoLayout() {
        var container = document.getElementById(_containerId);
        if (!container || !container._edges) return;

        var stepIds = Object.keys(_nodes);
        if (stepIds.length === 0) return;

        // Topological ordering via in-degree for horizontal placement
        var inDeg = {};
        var outAdj = {};
        stepIds.forEach(function (id) { inDeg[id] = 0; outAdj[id] = []; });
        (container._edges || []).forEach(function (e) {
            if (inDeg[e.targetStepId] !== undefined) inDeg[e.targetStepId]++;
            if (outAdj[e.sourceStepId]) outAdj[e.sourceStepId].push(e.targetStepId);
        });

        var layers = [];
        var assigned = {};
        var remaining = stepIds.slice();

        while (remaining.length > 0) {
            var layerNodes = remaining.filter(function (id) { return inDeg[id] === 0; });
            if (layerNodes.length === 0) {
                // cycle fallback: take first unassigned
                layerNodes = [remaining[0]];
            }
            layers.push(layerNodes);
            layerNodes.forEach(function (id) {
                assigned[id] = true;
                (outAdj[id] || []).forEach(function (tgt) { inDeg[tgt]--; });
            });
            remaining = remaining.filter(function (id) { return !assigned[id]; });
        }

        var xGap = 220;
        var yGap = 90;
        var marginLeft = 40;
        var marginTop = 40;

        layers.forEach(function (layerIds, col) {
            var totalH = layerIds.length * yGap;
            var startY = marginTop + (layerIds.length > 1 ? 0 : 0);
            layerIds.forEach(function (id, row) {
                var el = _nodes[id];
                if (!el) return;
                el.style.left = _px(marginLeft + col * xGap);
                el.style.top = _px(startY + row * yGap);
                if (_dotNetRef) {
                    _dotNetRef.invokeMethodAsync("OnNodeMovedJs", id,
                        marginLeft + col * xGap, startY + row * yGap);
                }
            });
        });

        requestAnimationFrame(function () { _redrawEdges(); });
    }

    function notifyEdgeCreated(sourceStepId, targetStepId) {
        if (_dotNetRef) _dotNetRef.invokeMethodAsync("OnEdgeCreatedJs", sourceStepId, targetStepId);
    }

    function notifyNodeDeleted(stepId) {
        if (_dotNetRef) _dotNetRef.invokeMethodAsync("OnNodeDeletedJs", stepId);
    }

    return {
        initCanvas: initCanvas,
        updateLayout: updateLayout,
        autoLayout: autoLayout,
        notifyEdgeCreated: notifyEdgeCreated,
        notifyNodeDeleted: notifyNodeDeleted
    };
})();
