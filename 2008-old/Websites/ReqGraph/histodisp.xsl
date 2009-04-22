<?xml version="1.0" encoding="utf-8" ?> 
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:template match="/">
  <xsl:variable name="divisions">
   <xsl:if test="/datafreq/@divisions"><xsl:value-of select="/datafreq/@divisions"/></xsl:if>
   <xsl:if test="not(/datafreq/@divisions)"><xsl:value-of select="100"/></xsl:if>
  </xsl:variable>
  <xsl:variable name="start">
   <xsl:if test="/datafreq/@start"><xsl:value-of select="/datafreq/@start"/></xsl:if>
   <xsl:if test="not(/datafreq/@start)"><xsl:value-of select="0"/></xsl:if>
  </xsl:variable>
  <xsl:variable name="end">
   <xsl:if test="/datafreq/@end"><xsl:value-of select="/datafreq/@end"/></xsl:if>
   <xsl:if test="not(/datafreq/@end)"><xsl:value-of select="100"/></xsl:if>
  </xsl:variable>
  <html>
   <head><title>Histogram of request Density</title>
    <xsl:if test="/datafreq">
     <style type="text/css">
      td{
       border:solid Grey 1px;
      }
      .bar{
       width: 1px;
       display: block;
       background-color:Blue;
       color:Blue;
       position: absolute;
       float: left;
       bottom: 0px;
      }
     </style>
    </xsl:if>
   </head>
   <body>
    <h3>Request parameters</h3>
    <p><form method="get">
     Enter the number of divisions to display (1-9999): <input type="text" size="4" maxlength="4" name="divisions" value="{$divisions}"/><br/>
     Enter the sub-range to display: [<input type="text" size="4" maxlength="4" name="start" value="{$start}"/>, <input type="text" size="4" maxlength="4" name="end" value="{$end}"/>)<br/>
     <input type="submit" value="Generate Histogram"/>
    </form></p>
    <xsl:apply-templates select="error"/>
    <xsl:apply-templates select="datafreq"/>
   </body>
  </html>
 </xsl:template>
 <xsl:template match="error">
  <div style="font-family:Sans-Serif;color:Red;font-weight:bold"><xsl:value-of select="."/></div>
 </xsl:template>
 <xsl:template match="datafreq">
 <xsl:variable name="colcount" select="(number(/datafreq/@end)-number(/datafreq/@start))"/>
  <xsl:variable name="maxval"><xsl:value-of select="@maxcount" /></xsl:variable>
  <h3>Frequency Histogram.</h3>
  <div style="position:relative;width:{$colcount}px;height:500px; border:solid 2px Black; background-color:Yellow;vertical-align:bottom">
   <xsl:for-each select="data">
    <xsl:variable name="ys"><xsl:value-of select="(number(@val) * 500) div $maxval"/></xsl:variable>
    <div class="bar" style="height:{$ys}px;left:{number(@num)-number(../@start)}px;" id="bar{@num}"></div>
   </xsl:for-each>
  </div>
  <ul>
   <li><xsl:value-of select="@valcount"/> requests in log.</li>
   <li><xsl:value-of select="@subcount"/> requests in displayed subsection.</li>
  </ul>
 </xsl:template>
</xsl:stylesheet>