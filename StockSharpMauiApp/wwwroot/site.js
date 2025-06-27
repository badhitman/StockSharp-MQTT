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