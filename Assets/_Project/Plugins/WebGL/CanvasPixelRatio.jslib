mergeInto(LibraryManager.library, {
    UI_GetCanvasPixelRatio: function () {
        var canvas = Module.canvas;
        if (canvas && canvas.clientWidth > 0) {
            return canvas.width / canvas.clientWidth;
        }
        return window.devicePixelRatio || 1;
    }
});
