// elbruno.Doc2Code — JS interop: auto-scrolls the console log panel to its latest entry.

window.doc2codeScrollToEnd = function (panelId) {
    const panel = document.getElementById(panelId);
    if (panel) {
        panel.scrollTop = panel.scrollHeight;
    }
};
