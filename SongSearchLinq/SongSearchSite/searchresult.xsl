<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" method="html"/>
  <xsl:template match="/">
    <html>
      <head>
        <link rel="Stylesheet" type="text/css" href="songsearch.css" />
        <link rel="Stylesheet" type="text/css" href="tableview.css" />
        <!--#8ba;#c4ddd5;#e2eeea-->
      </head>
      <body>
        <table>
          <colgroup>
            <col/>
            <col />
            <col align="right" style="width:2em; text-align:right;" />
            <col align="char" char=":" style="width:3em"/>
            <col />
          </colgroup>
          <thead>
            <tr>
              <th>Artist</th>
              <th>Title</th>
              <th>#</th>
              <th>Time</th>
              <th>Album</th>
            </tr>
          </thead>
          <tbody id="listdata">
            <xsl:apply-templates select="*"/>
          </tbody>
        </table>
      </body>
      <script type="text/javascript">
        
        document.getElementById("listdata").addEventListener('click', parent.SearchListClicked, true);
      </script>
    </html>
  </xsl:template>

  <xsl:template match="songs">
    <xsl:apply-templates select="*"/>
  </xsl:template>

  <xsl:template match="partsong|songref|song">
    <xsl:variable name="songlabel">
      <xsl:apply-templates select="." mode="makelabel" />
    </xsl:variable>
    <tr data-href="{@songuri}" data-songlabel="{$songlabel}">
      <xsl:choose>
        <xsl:when test="@artist">
          <td>
            <div>
              <span>
                <xsl:value-of select="@artist"/>
              </span>
              <i>
                <i>
                  ............ ?<br/>............ ?<br/>............ ?
                </i>
              </i>
            </div>
          </td>
          <td>
            <div>
              <span>
                <xsl:value-of select="@title"/>
              </span>
              <i>
                <i>
                  ............ ?<br/>............ ?<br/>............ ?
                </i>
              </i>
            </div>
          </td>
          <td>
            <div style="text-align:right;margin-right:0.5em;">
              <span>
                <xsl:value-of select="@track"/>
              </span>
              <i>
                <i>
                  ............ ?<br/>............ ?<br/>............ ?
                </i>
              </i>
            </div>
          </td>
          <td>
            <div style="text-align:right;margin-right:0.5em;">
              <span>
                <xsl:value-of select="concat(number(floor(number(@length) div 60)),':',substring('0',floor(number(@length) mod 60 div 10) +1), string(number(@length) mod 60))"/>
              </span>
              <i>
                <i>
                  ............ ?<br/>............ ?<br/>............ ?
                </i>
              </i>
            </div>
          </td>
          <td>
            <div>
              <span>
                <xsl:value-of select="@album"/>
              </span>
              <i>
                <i>
                  ............ ?<br/>............ ?<br/>............ ?
                </i>
              </i>
            </div>
          </td>
        </xsl:when>
        <xsl:otherwise>
          <td colspan="5" >
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