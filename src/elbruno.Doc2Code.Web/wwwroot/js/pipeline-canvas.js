// elbruno.Doc2Code — JS interop for the pipeline designer canvas (node drag, SVG edges, layout, connector ports).

window.pipelineCanvas = (function () {
    "use strict";

    let _dotNetRef = null;
    let _containerId = "";
    let _nodes = {};
    let _svgEl = null;
    let _dragState = null;

    // ── Connection drag state ──
    let _connectState = { sourceStepId: null, active: false };
    let _tempLine = null;

    // ── Selected edge state ──
    let _selectedEdge = null;  // { sourceStepId, targetStepId }

    // ── Bookend step ID constants ──
    var STEP_DOCUMENT_INPUT = "step-document-input";
    var STEP_GENERATED_ASSETS = "step-generated-assets";

    function _px(v) { return v + "px"; }

    function _buildNodeEl(step, agentName) {
        const el = document.createElement("div");
        el.className = "pd-node";
        el.dataset.stepId = step.stepId;
        el.style.left = _px(step.positionX);
        el.style.top = _px(step.positionY);

        // Bookend styling
        if (step.isBookend) {
            el.classList.add("pd-node-bookend");
        }

        var badgeHtml = "";
        if (step.isBookend) {
            var isStart = step.stepId === STEP_DOCUMENT_INPUT;
            badgeHtml = '<span class="pd-bookend-badge">' + (isStart ? "START" : "END") + '</span>';
        }

        el.innerHTML =
            '<span class="pd-node-label">' + _escHtml(agentName || step.agentKey) + '</span>' +
            badgeHtml +
            (step.retryPolicy ? '<span class="pd-retry-badge" title="Retry policy">&#x21bb; ' + step.retryPolicy.maxRetries + '</span>' : '');

        // Add connector ports (bookend exceptions)
        var isDocInput = step.stepId === STEP_DOCUMENT_INPUT;
        var isGenAssets = step.stepId === STEP_GENERATED_ASSETS;

        if (!isGenAssets) {
            var outPort = document.createElement("div");
            outPort.className = "pd-port pd-port-out";
            outPort.dataset.port = "out";
            outPort.dataset.stepId = step.stepId;
            outPort.addEventListener("pointerdown", function (ev) {
                ev.stopPropagation();
                ev.preventDefault();
                _startConnect(step.stepId, ev);
            });
            el.appendChild(outPort);
        }

        if (!isDocInput) {
            var inPort = document.createElement("div");
            inPort.className = "pd-port pd-port-in";
            inPort.dataset.port = "in";
            inPort.dataset.stepId = step.stepId;
            inPort.addEventListener("pointerup", function (ev) {
                ev.stopPropagation();
                if (_connectState.active && _connectState.sourceStepId !== step.stepId) {
                    _finishConnect(step.stepId);
                }
            });
            inPort.addEventListener("pointerenter", function () {
                if (_connectState.active && _connectState.sourceStepId !== step.stepId) {
                    inPort.classList.add("pd-port-valid-target");
                }
            });
            inPort.addEventListener("pointerleave", function () {
                inPort.classList.remove("pd-port-valid-target");
            });
            el.appendChild(inPort);
        }

        el.addEventListener("pointerdown", function (ev) {
            if (ev.target.classList.contains("pd-port")) return;
            _startDrag(ev, step.stepId);
        });
        el.addEventListener("click", function (ev) {
            if (ev.target.classList.contains("pd-port")) return;
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
        _selectedEdge = null;
        _redrawEdges();
        if (_dotNetRef) _dotNetRef.invokeMethodAsync("OnNodeSelectedJs", stepId);
    }

    function _selectEdge(sourceStepId, targetStepId) {
        _selectedEdge = { sourceStepId: sourceStepId, targetStepId: targetStepId };
        // Deselect any selected node
        var container = document.getElementById(_containerId);
        if (container) {
            container.querySelectorAll(".pd-node-selected").forEach(function (n) {
                n.classList.remove("pd-node-selected");
            });
        }
        _redrawEdges();
        if (_dotNetRef) _dotNetRef.invokeMethodAsync("OnEdgeSelectedJs", sourceStepId, targetStepId);
    }

    function _deselectAll() {
        _selectedEdge = null;
        var container = document.getElementById(_containerId);
        if (container) {
            container.querySelectorAll(".pd-node-selected").forEach(function (n) {
                n.classList.remove("pd-node-selected");
            });
        }
        _redrawEdges();
        if (_dotNetRef) _dotNetRef.invokeMethodAsync("OnDeselectAllJs");
    }

    function _deleteSelectedEdge() {
        if (!_selectedEdge || !_dotNetRef) return;
        _dotNetRef.invokeMethodAsync("OnEdgeDeletedJs", _selectedEdge.sourceStepId, _selectedEdge.targetStepId);
        _selectedEdge = null;
    }

    // ── Node drag ──

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

    // ── Port-to-port connection drag ──

    function _startConnect(sourceStepId, ev) {
        _connectState.sourceStepId = sourceStepId;
        _connectState.active = true;

        var container = document.getElementById(_containerId);
        if (!container || !_svgEl) return;

        var coords = _nodePortCoords(sourceStepId);
        if (!coords) return;
        var startX = coords.portOutX;
        var startY = coords.portOutY;

        _tempLine = document.createElementNS("http://www.w3.org/2000/svg", "line");
        _tempLine.setAttribute("x1", startX);
        _tempLine.setAttribute("y1", startY);
        _tempLine.setAttribute("x2", startX);
        _tempLine.setAttribute("y2", startY);
        _tempLine.setAttribute("class", "pd-edge-temp");
        _svgEl.appendChild(_tempLine);

        document.addEventListener("pointermove", _onConnectMove);
        document.addEventListener("pointerup", _onConnectCancel);
    }

    function _onConnectMove(ev) {
        if (!_connectState.active || !_tempLine) return;
        var container = document.getElementById(_containerId);
        if (!container) return;
        var cRect = container.getBoundingClientRect();
        var mx = ev.clientX - cRect.left;
        var my = ev.clientY - cRect.top;
        _tempLine.setAttribute("x2", mx);
        _tempLine.setAttribute("y2", my);
    }

    function _onConnectCancel() {
        _cleanupConnect();
    }

    function _finishConnect(targetStepId) {
        if (!_connectState.active) return;
        var sourceStepId = _connectState.sourceStepId;
        _cleanupConnect();
        if (_dotNetRef && sourceStepId && targetStepId) {
            _dotNetRef.invokeMethodAsync("OnEdgeCreatedJs", sourceStepId, targetStepId);
        }
    }

    function _cleanupConnect() {
        _connectState.active = false;
        _connectState.sourceStepId = null;
        if (_tempLine && _tempLine.parentNode) {
            _tempLine.parentNode.removeChild(_tempLine);
        }
        _tempLine = null;
        // Remove any lingering valid-target highlights
        var container = document.getElementById(_containerId);
        if (container) {
            container.querySelectorAll(".pd-port-valid-target").forEach(function (p) {
                p.classList.remove("pd-port-valid-target");
            });
        }
        document.removeEventListener("pointermove", _onConnectMove);
        document.removeEventListener("pointerup", _onConnectCancel);
    }

    // ── Port-aware coordinate calculation ──

    function _nodePortCoords(stepId) {
        var el = _nodes[stepId];
        if (!el) return null;
        var x = parseFloat(el.style.left) || 0;
        var y = parseFloat(el.style.top) || 0;
        var w = el.offsetWidth || 140;
        var h = el.offsetHeight || 48;
        return {
            cx: x + w / 2,
            cy: y + h / 2,
            right: x + w,
            left: x,
            top: y,
            bottom: y + h,
            portOutX: x + w + 6,
            portOutY: y + h / 2,
            portInX: x - 6,
            portInY: y + h / 2
        };
    }

    function _redrawEdges() {
        if (!_svgEl) return;
        // Preserve temp line if present
        var tempRef = _tempLine;
        while (_svgEl.firstChild) _svgEl.removeChild(_svgEl.firstChild);
        if (tempRef) _svgEl.appendChild(tempRef);

        var container = document.getElementById(_containerId);
        if (!container || !container._edges) return;
        container._edges.forEach(function (edge) {
            var src = _nodePortCoords(edge.sourceStepId);
            var tgt = _nodePortCoords(edge.targetStepId);
            if (!src || !tgt) return;

            var startX = src.portOutX;
            var startY = src.portOutY;
            var endX = tgt.portInX;
            var endY = tgt.portInY;
            var midX = (startX + endX) / 2;

            var pathData = "M " + startX + " " + startY +
                " C " + midX + " " + startY + ", " + midX + " " + endY + ", " + endX + " " + endY;

            var isSelected = _selectedEdge &&
                _selectedEdge.sourceStepId === edge.sourceStepId &&
                _selectedEdge.targetStepId === edge.targetStepId;

            // Invisible wide hit-area path for easier clicking
            var hitEl = document.createElementNS("http://www.w3.org/2000/svg", "path");
            hitEl.setAttribute("d", pathData);
            hitEl.setAttribute("fill", "none");
            hitEl.setAttribute("stroke", "transparent");
            hitEl.setAttribute("stroke-width", "14");
            hitEl.setAttribute("cursor", "pointer");
            hitEl.setAttribute("pointer-events", "stroke");
            (function (eSrc, eTgt) {
                hitEl.addEventListener("click", function (ev) {
                    ev.stopPropagation();
                    _selectEdge(eSrc, eTgt);
                });
            })(edge.sourceStepId, edge.targetStepId);
            _svgEl.appendChild(hitEl);

            var pathEl = document.createElementNS("http://www.w3.org/2000/svg", "path");
            pathEl.setAttribute("d", pathData);
            pathEl.setAttribute("class", isSelected ? "pd-edge-path pd-edge-selected" : "pd-edge-path");
            pathEl.setAttribute("pointer-events", "none");
            _svgEl.appendChild(pathEl);

            // arrowhead triangle at the end
            var arrowLen = 10;
            var angle = Math.atan2(endY - startY, endX - startX);
            if (Math.abs(endX - startX) > 10) angle = 0; // mostly horizontal edges
            var ax1 = endX - arrowLen * Math.cos(angle - 0.4);
            var ay1 = endY - arrowLen * Math.sin(angle - 0.4);
            var ax2 = endX - arrowLen * Math.cos(angle + 0.4);
            var ay2 = endY - arrowLen * Math.sin(angle + 0.4);
            var arrowPath = "M " + endX + " " + endY + " L " + ax1 + " " + ay1 + " L " + ax2 + " " + ay2 + " Z";
            var arrowEl = document.createElementNS("http://www.w3.org/2000/svg", "path");
            arrowEl.setAttribute("d", arrowPath);
            arrowEl.setAttribute("fill", isSelected ? "#ff6b6b" : "#33ff77");
            arrowEl.setAttribute("stroke", "none");
            arrowEl.setAttribute("pointer-events", "none");
            _svgEl.appendChild(arrowEl);

            // Edge label (OutputKeyMapping) at Bézier midpoint
            if (edge.outputKeyMapping) {
                var labelX = (startX + endX) / 2;
                var labelY = (startY + endY) / 2 - 6;
                var textEl = document.createElementNS("http://www.w3.org/2000/svg", "text");
                textEl.setAttribute("x", labelX);
                textEl.setAttribute("y", labelY);
                textEl.setAttribute("text-anchor", "middle");
                textEl.setAttribute("fill", isSelected ? "#ff6b6b" : "#6e7681");
                textEl.setAttribute("font-size", "10");
                textEl.setAttribute("font-family", "'Cascadia Code', monospace");
                textEl.setAttribute("pointer-events", "none");
                textEl.textContent = edge.outputKeyMapping;
                _svgEl.appendChild(textEl);

                // Delete badge on selected edge
                if (isSelected) {
                    var delX = labelX + 4;
                    var delY = labelY + 14;
                    var delText = document.createElementNS("http://www.w3.org/2000/svg", "text");
                    delText.setAttribute("x", delX);
                    delText.setAttribute("y", delY);
                    delText.setAttribute("text-anchor", "middle");
                    delText.setAttribute("fill", "#ff6b6b");
                    delText.setAttribute("font-size", "10");
                    delText.setAttribute("font-family", "'Cascadia Code', monospace");
                    delText.setAttribute("pointer-events", "visiblePainted");
                    delText.setAttribute("cursor", "pointer");
                    delText.textContent = "\u2716 disconnect";
                    delText.addEventListener("click", function (ev) {
                        ev.stopPropagation();
                        _deleteSelectedEdge();
                    });
                    _svgEl.appendChild(delText);
                }
            }
        });
    }

    // ── Public API ─────────────────────────────────────────────────

    function initCanvas(dotNetRef, containerId, pipelineJson) {
        _dotNetRef = dotNetRef;
        _containerId = containerId;
        _nodes = {};
        _cleanupConnect();

        var container = document.getElementById(containerId);
        if (!container) return;
        // Clear previous content
        container.innerHTML = "";

        var pipeline = typeof pipelineJson === "string" ? JSON.parse(pipelineJson) : pipelineJson;

        // Create SVG overlay for edges
        _svgEl = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        _svgEl.setAttribute("class", "pd-svg-overlay");
        _svgEl.style.pointerEvents = "none";
        container.appendChild(_svgEl);

        // Click on empty canvas deselects everything
        container.addEventListener("click", function (ev) {
            if (ev.target === container || ev.target === _svgEl) {
                _deselectAll();
            }
        });

        // Keyboard handler for Delete/Backspace to remove selected edge
        if (!container._keyHandler) {
            container._keyHandler = function (ev) {
                if (ev.key === "Delete" || ev.key === "Backspace") {
                    // Don't intercept if user is typing in an input
                    var tag = (ev.target.tagName || "").toLowerCase();
                    if (tag === "input" || tag === "textarea" || tag === "select") return;
                    if (_selectedEdge) {
                        ev.preventDefault();
                        _deleteSelectedEdge();
                    }
                }
                if (ev.key === "Escape") {
                    _deselectAll();
                }
            };
            document.addEventListener("keydown", container._keyHandler);
        }

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
