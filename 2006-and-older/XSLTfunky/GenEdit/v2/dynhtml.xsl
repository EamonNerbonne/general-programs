<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" 
     xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:output method="xml" indent="yes"/>
 <xsl:template match="editor">
  <xsl:variable name="top" select="@toprow"/>
  <xsl:variable name="bottom" select="number(@toprow)+number(@numrowsvisible)"/>
  <table class="genedit">
   <xsl:copy-of select="thead"/>
   <tbody>
    <xsl:for-each select="rows/row">
     <xsl:variable name="show" select="(position()&gt;=$top) and (position()&lt;$bottom)"/>
     <xsl:apply-templates select=".">
      <xsl:with-param name="show" select="$show"/>  
     </xsl:apply-templates>
    </xsl:for-each>
   </tbody>
   <xsl:copy-of select="tfoot"/>   
  </table>
 </xsl:template>

 <xsl:template match="row">
  <xsl:param name="show"/>
  <xsl:apply-templates select="/editor/rowtemplate">
   <xsl:with-param name="row" select="."/>
   <xsl:with-param name="show" select="$show"/>
  </xsl:apply-templates>
 </xsl:template>

 <xsl:template match="rowtemplate">
  <xsl:param name="row"/>
  <xsl:param name="show"/>
  <xsl:apply-templates select="*|text()" mode="rt">
   <xsl:with-param name="row" select="$row"/>
   <xsl:with-param name="show" select="$show"/>
  </xsl:apply-templates>
 </xsl:template>
 
 <xsl:template match="*|text()|@*" mode="rt">
  <xsl:param name="row"/>
  <xsl:param name="show"/>
  <xsl:copy>
   <xsl:if test="name(.)='tr'">
    <xsl:attribute name="class"><xsl:choose>
     <xsl:when test="$show">XSLGenEdit_show-row</xsl:when>
     <xsl:otherwise>XSLGenEdit_hide-row</xsl:otherwise>
    </xsl:choose></xsl:attribute>
   </xsl:if>
   <xsl:apply-templates mode="rt" select="*|text()|@*">
    <xsl:with-param name="row" select="$row"/>
    <xsl:with-param name="show" select="$show"/>
   </xsl:apply-templates>
  </xsl:copy>
 </xsl:template>
 
 <xsl:template match="input" mode="rt">
  <xsl:param name="row"/><!--refers to the row for which this was called-->
  <xsl:param name="show"/>
  <xsl:variable name="colref" select="@colref"/>
  <xsl:variable name="val" select="$row/val[@name=$colref]"/>
  <input value="{$val/@new}">
   <xsl:attribute name="rowref"><xsl:value-of select="$row/@id"/></xsl:attribute>
   <xsl:copy-of select="@*[name()!='class' and name()!='value']"/>
   <xsl:choose>
    <!--<xsl:when test="contains(concat(';',($val/@flags)),';focus;')">
     <xsl:attribute name="class">XSLGenEdit_focused</xsl:attribute>
    </xsl:when>-->
    <xsl:when test="string($val/@new)!=string($val/@old)">
     <xsl:attribute name="class">XSLGenEdit_changed</xsl:attribute>
    </xsl:when>
    <xsl:otherwise>
     <xsl:attribute name="class">XSLGenEdit_unchanged</xsl:attribute>
    </xsl:otherwise>
   </xsl:choose>
  </input>
 </xsl:template>
</xsl:stylesheet>