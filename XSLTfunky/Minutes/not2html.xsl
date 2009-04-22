<?xml version="1.0" encoding="utf-8" ?> 
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" omit-xml-declaration="no" doctype-public="-//W3C//DTD XHTML 1.0 Strict//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd" indent="no"/>
<xsl:template match="notulen">
 <html><!-- xmlns="http://www.w3.org/1999/xhtml"-->
  <head>
   <link rel="stylesheet" type="text/css" href="not.css"/>
   <title>Meeting on <xsl:value-of select="head/date"/></title>
  </head>
  <body>
   <xsl:apply-templates select="head"/>
   <xsl:apply-templates select="main"/>
  </body>
 </html>
</xsl:template>
<xsl:template match="head">
 <h1>"Afstudeerproject" meeting minutes</h1>
 <p>
  <table class="wwwtab"><tbody>
   <tr><td class="www">When:</td><td class="wwwdata"><xsl:value-of select="date"/></td></tr>
   <tr><td class="www">Where:</td><td class="wwwdata"><xsl:value-of select="place"/></td></tr>
   <tr><td class="www">Who:</td><td class="wwwdata"><xsl:value-of select="people"/></td></tr>
  </tbody></table>
 </p>
 <xsl:apply-templates select="preface"/>
</xsl:template>

<xsl:template match="preface">
 <p>
  <xsl:apply-templates select="*|text()" mode="copyall"/>
 </p>
</xsl:template>

<xsl:template match="main">
 <ol class="main-list">
  <xsl:apply-templates select="section" mode="tlist"/>
 </ol>
</xsl:template>

<xsl:template match="section|item" mode="tlist">
 <li class="tlistitem">
   <xsl:copy-of select="@value"/>
   <div class="{name(.)}-name"><xsl:value-of select="@name"/></div>:
   <xsl:apply-templates select="*|text()" mode="copyall"/>
  <div class="{name(.)}-separator"></div>
 </li>
</xsl:template>

<xsl:template match="*|text()|@*" mode="copyall">
 <xsl:copy>
  <xsl:apply-templates select="*|text()|@*" mode="copyall"/>
 </xsl:copy>
</xsl:template>

<xsl:template match="tlist" mode="copyall">
 <xsl:element name="{@type}">
  <xsl:attribute name="class"><xsl:value-of select="name(.)"/>-list</xsl:attribute>
  <xsl:apply-templates select="item|section" mode="tlist"/>
 </xsl:element>
</xsl:template>
</xsl:stylesheet>