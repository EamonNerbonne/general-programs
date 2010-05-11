$(document).ready(function ($) {
    var docEl = $("body")[0];
    var finSize = 12;
    var initSize = 2;
    var steps = 60;
    function doAnim() {
        var frameEl = $(window.frames['resultsview'].document.documentElement).children("body")[0];
        var i = 0;
        function dostep() {
            var r = i / steps;
            var newSize = (1 - r) * initSize + r * finSize;
            var bodP = newSize / finSize * 100;
            docEl.style.fontSize = newSize + 'pt';
            docEl.style.opacity = r + '';
            if(frameEl)
            frameEl.style.fontSize = newSize + 'pt';
            docEl.style.width = bodP + '%';
            docEl.style.height = bodP + '%';
            docEl.style.marginTop = (100 - bodP) / 2 + '%';
            docEl.style.marginLeft = (100 - bodP) / 2 + '%';
            if (i < steps) {
                i++;
                setTimeout(dostep, 20);
            }
        }

        dostep();
    }
    $("h3").click(doAnim);
});