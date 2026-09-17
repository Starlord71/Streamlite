// Bridge that lets the Razor UI ask the WPF host for a native Windows file or folder dialog.
// JavaScript is only the transport: a browser file input cannot produce a real filesystem
// path, so the pickers must run on the host, which exposes ShowOpenFileDialogAsync and
// ShowOpenFolderDialogAsync.
window.fileDialog = (function () {
    let hostBridge = null;

    return {
        setHostBridge: function (bridge) {
            hostBridge = bridge;
        },
        openFilePicker: function (filter, title) {
            if (!hostBridge) {
                return Promise.resolve(null);
            }

            return hostBridge.invokeMethodAsync("ShowOpenFileDialogAsync", filter, title);
        },
        openFolderPicker: function (title) {
            if (!hostBridge) {
                return Promise.resolve(null);
            }

            return hostBridge.invokeMethodAsync("ShowOpenFolderDialogAsync", title);
        }
    };
})();
