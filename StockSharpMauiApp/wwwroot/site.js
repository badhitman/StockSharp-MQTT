window.TradeInstrumentStrategy = {
    ButtonSplash: function (instrumentId) {
        // console.warn(`call -> TradeInstrumentStrategy.ButtonSplash(instrumentId:${instrumentId})`);
        var options = {};
        $(`#trade-instrument-${instrumentId}-row`).effect('highlight', options, 500);
        //$(`#trade-instrument-${instrumentId}-row`).animate({ color: 'red' }, 500);
    },
}

window.Toast = {
    Info: function (title, text) {
        //console.warn(`call -> Toast.Info(text:${text})`);
        $.toast({
            heading: title,
            text: text,
            icon: 'info'
        })
    },
    Warning: function (title, text) {
        //console.warn(`call -> Toast.Warning(text:${text})`);
        $.toast({
            heading: title,
            text: text,
            icon: 'warning'
        })
    },
    Error: function (title, text) {
        //console.warn(`call -> Toast.Error(text:${text})`);
        $.toast({
            heading: title,
            text: text,
            icon: 'error'
        })
    },
    Success: function (title, text) {
        //console.warn(`call -> Toast.Success(text:${text})`);
        $.toast({
            heading: title,
            text: text,
            icon: 'success'
        })
    }
}
function autoGrow(el) {
    if (el.style == undefined)
        return;

    el.style.height = '5px';
    el.style.height = el.scrollHeight + 'px';
}

window.autoGrowManage = (() => {
    return {
        registerGrow(dom_id, dotNetReference) {
            autoGrow(this);
            if (this.scrollHeight !== undefined)
                dotNetReference.invokeMethodAsync('EditorDataChanged', this.scrollHeight);
        }
    };
})();