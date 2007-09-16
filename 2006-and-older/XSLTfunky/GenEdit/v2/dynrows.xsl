<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:output method="xml" indent="yes"/>
 <xsl:template match="query">
  <editor toprow="1">
   <xsl:attribute name="numrowsvisible"><xsl:choose>
     <xsl:when test="define/@numrowsvisible"><xsl:value-of select="define/@numrowsvisible"/></xsl:when>
     <xsl:otherwise>20</xsl:otherwise>
    </xsl:choose></xsl:attribute>
   <xsl:copy-of select="define/thead"/>
   <xsl:copy-of select="define/tfoot"/>
   <xsl:copy-of select="define/rowtemplate"/>
   <!--ok, all "meta-data" is set up, now for the actual rows"-->
   <rows>
    <xsl:apply-templates select="data/row"/>
   </rows>
  </editor> 
 </xsl:template>

 <xsl:template match="row">
  <row id="{generate-id()}_XSLGenEdit">
   <xsl:variable name="fieldnames" select="/query/define/cols/col/@name"/>
   <xsl:for-each select="@*[name()=$fieldnames]">
    <val name="{name()}" new="{.}" old="{.}"/>   
   </xsl:for-each>
  </row>
 </xsl:template>

 <xsl:template match="row[1]">
  <row id="{generate-id() }_XSLGenEdit">
   <xsl:variable name="firstname" select="/query/define/cols/col[position()=1]/@name"/>
   <xsl:variable name="fieldnames" select="/query/define/cols/col[position()!=1]/@name"/>

   <xsl:for-each select="@*[name()=$firstname]">
    <val name="{name()}" new="{.}" old="{.}" flags="focus;"/>   
   </xsl:for-each>

   <xsl:for-each select="@*[name()=$fieldnames]">
    <val name="{name()}" new="{.}" old="{.}"/>   
   </xsl:for-each>
  </row>
 </xsl:template>
</xsl:stylesheet>