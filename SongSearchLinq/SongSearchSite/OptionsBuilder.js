//by Eamon Nerbonne 2010-05-02

(function ($) {
    var makeType = {
        checkbox: function (opt) { return $("<input type='checkbox'/>"); }
    };
    var getValue = {
        checkbox: function (el) { return el.attr("checked"); }
    };
    var setValue = {
        checkbox: function (el, val) { el.attr("checked", val); }
    };

    function generalSet(val) { //in context of option
        setValue[this.type](this.element, val);
    }
    function generalGet() {
        return setValue[this.type](this.element);
    }

    function changeHandler(e) {
        var sel = $(this);
        var opt = sel.data("OptionsBuilderOpt");
        if (opt.onchange) opt.onchange(getValue[opt.type](sel), e);
    }

    function procOpts(optionsEl, options) {
        var tab = $(document.createElement("table"));
        for (name in options) {
            var opt = options[name];
            opt.name = name;
            var row = $(document.createElement("tr"));
            $(document.createElement("label")).text(opt.label).attr("for", opt.name)
                .appendTo($(document.createElement("td")).appendTo(row));
            opt.element = makeType[opt.type](opt).attr("name", opt.name).data("OptionsBuilderOpt", opt)
                .appendTo($(document.createElement("td")).appendTo(row));
            opt.setValue = generalSet;
            opt.getValue = generalGet;
            opt.setValue(opt.initialValue);
            row.appendTo(tab);
            opt.element.change(changeHandler);
        }
        tab.appendTo(optionsEl);
        optionsEl.data("OptionsBuilderTable", tab);
        optionsEl.data("OptionsBuilder", options);
        optionsEl.removeClass("OptionsBuilder-uninitialized");
    }

    function killOpts(optionsEl) {
        var opts = optionsEl.data("OptionsBuilderTable");
        if (opts != undefined) {
            opts.remove();
            optionsEl.removeData("OptionsBuilderTable");
            optionsEl.removeData("OptionsBuilder");
        }
    }

    $.fn.OptionsBuilder = function (newOpts) {
        if (typeof newOpts == "object") {
            killOpts(this);
            procOpts(this, newOpts);
            //option must be an object.  each property is considered an option, and each option should have: label, type, initialValue, onchange(newval,e)
            //will be extended with: name, element, getValue, setValue
        }
        return this.data("OptionsBuilder");
    }
})(jQuery);