<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" method="html"/>
  <xsl:template match="/">
    <html>
      <head>
        <link rel="shortcut icon" type="image/ico" href="img/emnicon.ico" />
        <link rel="Stylesheet" type="text/css" href="css/tableview.css" />
        <!--#8ba;#c4ddd5;#e2eeea-->
      </head>
      <body>
        <xsl:apply-templates select="songs/ordering" mode="serialized"/>
        <table>
          <colgroup>
            <col  />
            <col />
            <col />
            <col align="char" char=":" style="width:3em"/>
            <col align="right" style="width:2em; text-align:right;" />
            <col />
          </colgroup>
          <thead>
            <tr id="listhead">
                <xsl:apply-templates select="songs/ordering" mode="sortarrow">
                  <xsl:with-param name="col" select="'Rating'"/>
                </xsl:apply-templates>
                <xsl:apply-templates select="songs/ordering" mode="sortarrow">
                  <xsl:with-param name="col" select="'Artist'"/>
                </xsl:apply-templates>
                <xsl:apply-templates select="songs/ordering" mode="sortarrow">
                  <xsl:with-param name="col" select="'Title'"/>
                </xsl:apply-templates>
                <xsl:apply-templates select="songs/ordering" mode="sortarrow">
                  <xsl:with-param name="col" select="'Time'"/>
                </xsl:apply-templates>
                <xsl:apply-templates select="songs/ordering" mode="sortarrow">
                  <xsl:with-param name="col" select="'TrackNumber'"/>
                  <xsl:with-param name="display" select="'#'"/>
                </xsl:apply-templates>
                <xsl:apply-templates select="songs/ordering" mode="sortarrow">
                  <xsl:with-param name="col" select="'Album'"/>
                </xsl:apply-templates>
            </tr>
          </thead>
          <tbody id="listdata">
            <xsl:apply-templates select="*"/>
          </tbody>
        </table>
      </body>
      <script type="text/javascript">
        ( function () {
        document.getElementById("listdata").addEventListener('click', parent.SearchListClicked, true);
        document.getElementById("listhead").addEventListener('click', parent.SetOrdering, true);
        })();
      </script>
    </html>
  </xsl:template>

  <xsl:template match="ordering" mode="serialized">
    <xsl:attribute name="data-ordering">
      <xsl:for-each select="col">
        <xsl:if test="@dir = 'desc'">
          <xsl:text>-</xsl:text>
        </xsl:if>
        <xsl:if test="@dir = 'asc'">
          <xsl:text>+</xsl:text>
        </xsl:if>
        <xsl:value-of select="@name"/>
        <xsl:if test="not(position()=last())">
          <xsl:text>,</xsl:text>
        </xsl:if>
      </xsl:for-each>
    </xsl:attribute>
  </xsl:template>

  <xsl:template match="ordering" mode="sortarrow">
    <xsl:param name="col"/>
    <xsl:param name="display" select="$col"/>
    <th data-colname="{$col}">
      <xsl:value-of select="$display"/>
    <xsl:for-each select="col[@name=$col]">
      <span style="font-size:smaller; color:Gray;">
      <xsl:if test="@dir = 'desc'">
        <xsl:text>&#9660; </xsl:text>
      </xsl:if>
      <xsl:if test="@dir = 'asc'">
        <xsl:text>&#9650; </xsl:text>
      </xsl:if>

      <xsl:value-of select="count(preceding-sibling::*)+1"/>
      </span>
    </xsl:for-each>
    </th>
  </xsl:template>


  <xsl:template match="songs">
    <xsl:apply-templates select="*"/>
  </xsl:template>

  <xsl:template name="stars">
    <xsl:param name="num" select="0"/>
    <xsl:if test="$num &gt; 0.5">
      <xsl:text>&#x2605;</xsl:text>
      <xsl:call-template name="stars">
        <xsl:with-param name="num" select="$num - 1"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template name="stringEllipses">
    <xsl:param name="str"/>
    <div>
      <span>
        <xsl:value-of select="$str"/>
      </span>
      <i>
        <i>
          ............ ?<br/>............ ?<br/>............ ?
        </i>
      </i>
    </div>
  </xsl:template>

  <xsl:template name="stringNoEllipses">
    <xsl:param name="str"/>
    <xsl:value-of select="$str"/>
  </xsl:template>

  <xsl:template match="partsong|songref|song">
    <xsl:variable name="songlabel">
      <xsl:apply-templates select="." mode="makelabel" />
    </xsl:variable>
    <tr data-href="{@songuri}" title="{@songuri}" data-label="{$songlabel}" data-length="{@length}" data-replaygain="{@Tgain}" data-rating="{@rating}">
      <xsl:choose>
        <xsl:when test="@artist">
          <td >
            <div style="position:relative; width:5em;top:0;left:0;background:none;">
              <div style="position:relative; z-index:1;">
                <xsl:if test="@rating">
                  <xsl:call-template name="stars">
                    <xsl:with-param name="num" select="number(@rating) "/>
                  </xsl:call-template>
                </xsl:if>
                <xsl:if test="not(@rating)">
                  &#160;
                </xsl:if>
              </div>
              <xsl:if test="@popA">
                <xsl:variable name="width" select="number(@popA) *5"/>
                <div class="popAbar" style="width: {$width}em"/>
              </xsl:if>
              <xsl:if test="@popT">
                <xsl:variable name="width" select="number(@popT) *5"/>
                <div class="popTbar" style="width: {$width}em"/>
              </xsl:if>
            </div>
          </td>
          <td>
            <xsl:call-template name="stringNoEllipses">
              <xsl:with-param name="str" select="@artist"/>
            </xsl:call-template>
          </td>
          <td>
            <xsl:call-template name="stringNoEllipses">
              <xsl:with-param name="str" select="@title"/>
            </xsl:call-template>
          </td>
          <td  style="text-align:right;padding-right:0.5em;">
            <xsl:call-template name="stringNoEllipses">
              <xsl:with-param name="str" select="concat(number(floor(number(@length) div 60)),':',substring('0',floor(number(@length) mod 60 div 10) +1), string(number(@length) mod 60))"/>
            </xsl:call-template>
          </td>
          <td  style="text-align:right;padding-right:0.5em;">
            <xsl:call-template name="stringNoEllipses">
              <xsl:with-param name="str" select="@track"/>
            </xsl:call-template>
          </td>
          <td>
            <xsl:call-template name="stringNoEllipses">
              <xsl:with-param name="str" select="@album"/>
            </xsl:call-template>
          </td>
        </xsl:when>
        <xsl:otherwise>
          <td colspan="6" >
            <xsl:choose>
              <xsl:when test="string-length(@label) &gt; 0">
                <xsl:value-of select="@label"/>
              </xsl:when>
              <xsl:otherwise>
                <xsl:call-template name="replace">
                  <xsl:with-param name="str" select="@songuri"/>
                  <xsl:with-param name="what" >
                    <xsl:text>%20</xsl:text>
                  </xsl:with-param>
                  <xsl:with-param name="with" >
                    <xsl:text> </xsl:text>
                  </xsl:with-param>
                </xsl:call-template>
              </xsl:otherwise>
            </xsl:choose>
          </td>
        </xsl:otherwise>
      </xsl:choose>
    </tr>
  </xsl:template>


  <xsl:template match="partsong|songref|song" mode="makelabel">
    <xsl:choose>
      <xsl:when test="@artist">
        <xsl:value-of select="concat(@artist,' - ',@title)"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@label"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="replace">
    <xsl:param name="str" />
    <xsl:param name ="what"/>
    <xsl:param name="with"/>
    <xsl:choose>
      <xsl:when test="contains($str,$what)">
        <xsl:variable name="restRep">
          <xsl:call-template name="replace">
            <xsl:with-param name="str" select="substring-after($str,$what)"/>
            <xsl:with-param name="what" select="$what"/>
            <xsl:with-param name="with" select="$with"/>
          </xsl:call-template>
        </xsl:variable>
        <xsl:value-of select="concat(substring-before($str,$what),$with,$restRep)"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$str"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>