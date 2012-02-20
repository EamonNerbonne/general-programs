//by Eamon Nerbonne
function getSectionScope(jqElement) {
    return jqElement.parents().filter("html, article, section").first();
}

function getArticleScope(jqElement) {
    return jqElement.parents().filter("html, article").first();
}


function getSectionHeading(jqSection) {
    return jqSection.find("h1").filter(function () {
        return $(this).parents().filter("html, article, section").first()[0] === jqSection[0];
    }).first();
}

function createSectionToc(arrChildren) {
    var tableEl = document.createElement("table");
    for (var i = 0; i < arrChildren.length; ++i) {
        var jqSection = arrChildren[i];
        var sectionData = jqSection.data();
        var sectionHeading = getSectionHeading(jqSection);
        var sectionNumber = sectionData.fullpath;
        if (!sectionHeading || !sectionHeading.length)
            throw "Cannot process section (no section h1):" + jqSection.text();
        var headingString = sectionHeading[0].textContent;

        var numEl = document.createElement("td");
        numEl.appendChild(document.createTextNode(sectionNumber));

        var linkEl = document.createElement("a");
        linkEl.setAttribute("href", "#" + jqSection.attr("id"));
        linkEl.appendChild(document.createTextNode(headingString));
        var contentEl = document.createElement("td");
        contentEl.appendChild(linkEl);



        if (sectionData.childSections && sectionData.childSections.length > 0)
            contentEl.appendChild(createSectionToc(sectionData.childSections));
        var rowEl = document.createElement("tr");
        rowEl.appendChild(numEl);
        rowEl.appendChild(contentEl);
        tableEl.appendChild(rowEl);
    }

    return tableEl;
}

$(function () {

    $("section").each(function (index) {
        var thisSection = $(this);
        var thisSectionData = thisSection.data();
        if (thisSection.attr("id") === undefined) thisSection.attr("id", "autoSecNum" + index);

        var parentSection = getSectionScope(thisSection);
        var parentSectionData = parentSection.data();
        if (parentSectionData.childSections === undefined) parentSectionData.childSections = [];

        parentSectionData.childSections.push(thisSection);
        thisSectionData.sectionNumber = parentSectionData.childSections.length;
        thisSectionData.parentSection = parent;

        thisSectionData.fullpath = (parentSectionData.fullpath ? parentSectionData.fullpath + "." : "") + thisSectionData.sectionNumber;
        var sectionHeader = getSectionHeading(thisSection);

        sectionHeader.attr("data-fullpath", thisSectionData.fullpath);
    });

    $("div.generateTableOfContents").each(function () {
        var scope = getSectionScope($(this));

        var scopeData = scope.data();
        var arrChildren = scopeData.childSections;
        if (arrChildren && arrChildren.length > 0) {
            $(this).append(createSectionToc(arrChildren));
        }
    });

    $("figure").each(function (index) {
        var thisFigure = $(this);
        if (thisFigure.attr("id") === undefined) thisFigure.attr("id", "autoFigNum" + index);

        var parentArticle = getArticleScope(thisFigure);
        var parentArticleData = parentArticle.data();
        if (parentArticleData.figureCounter === undefined) parentArticleData.figureCounter = 0;
        parentArticleData.figureCounter++;
        thisFigure.children("figcaption").andSelf().each(function () {
            $(this).attr("data-figureNumber", parentArticleData.figureCounter);
        });
    });

    $("aside.bibliography li").each(function (index) {
        var thisCitation = $(this);
        if (thisCitation.attr("id") === undefined) thisCitation.attr("id", "autoCiteNum" + index);

        var parentArticle = getArticleScope(thisCitation);
        var parentArticleData = parentArticle.data();
        if (parentArticleData.citationCounter === undefined) parentArticleData.citationCounter = 0;
        parentArticleData.citationCounter++;
        thisCitation.attr("data-citationNumber", "" + parentArticleData.citationCounter);
    });

    $("a").filter('[href^="#"]').each(function () {
        if ($(this).text() == '[ref]') {
            var idOfRef = $(this).attr("href").substring(1);
            var referencedElement = $(document.getElementById(idOfRef));
            //might be a section, figure, equation or bibliography element
            if (referencedElement.length === 0) {
                $(this).addClass('ref-error');
                $(this).attr('data-err-reason', "Invalid reference");
            } else if (referencedElement[0].nodeName === 'SECTION') {
                var heading = getSectionHeading(referencedElement);
                var headingString = heading[0].textContent;
                if (!$(this).attr("title")) $(this).attr("title", headingString.trim());
                $(this).text('Section ' + referencedElement.data().fullpath);
            } else if (referencedElement[0].nodeName === 'LI' && referencedElement.parents().filter("aside.bibliography").length) {
                $(this).text('[' + referencedElement.attr('data-citationNumber') + ']');
                //in bib
            } else if (referencedElement[0].nodeName === 'FIGURE') {
                //figref
                $(this).text('Figure ' + referencedElement.attr('data-figureNumber') + '');
            } else if (referencedElement[0].nodeName === 'SCRIPT') {
                //in bib
                throw "Formula cross referencing not yet implemented";
            } else {
                $(this).addClass('ref-error');
                $(this).attr('data-err-reason', "referenced element unrecognized");
            }
        }
    });

});
      
