// elbruno.Doc2Code — JS interop: triggers a browser file download from a base64-encoded byte array.

window.doc2codeSaveFile = function (outputName, encodedData) {
    const raw = atob(encodedData);
    const bytes = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) {
        bytes[i] = raw.charCodeAt(i);
    }

    const blob = new Blob([bytes], { type: 'application/octet-stream' });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = outputName;
    document.body.appendChild(anchor);
    anchor.click();

    URL.revokeObjectURL(url);
    document.body.removeChild(anchor);
};

// elbruno.Doc2Code — JS interop: triggers a browser file download from a plain text string.
window.doc2codeSaveText = function (outputName, textContent) {
    const blob = new Blob([textContent], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = outputName;
    document.body.appendChild(anchor);
    anchor.click();

    URL.revokeObjectURL(url);
    document.body.removeChild(anchor);
};
