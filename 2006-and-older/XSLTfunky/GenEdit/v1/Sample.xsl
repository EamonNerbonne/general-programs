<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="/genedit">
  <form id="{@formid}" onsubmit="saveedit();return false">
   <table border="1" width="100%">
    <tr>
     <xsl:for-each select="@*">
      <xsl:sort select="name()"/>
      <th><xsl:value-of select="."/></th>
     </xsl:for-each>
    </tr>
    <xsl:apply-templates select="row"/>
   </table>
  </form>
 </xsl:template>
 <xsl:template match="row">
  <tr>
   <xsl:for-each select="@*">
    <xsl:sort select="name()"/>
    <td><xsl:value-of select="."/></td>
   </xsl:for-each>
  </tr>   
 </xsl:template>
</xsl:stylesheet>