<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
 <xsl:template match="definition">
  <table class="XSLGE">
   <thead>
    <tr><th colspan="{count(cols/col)}"><div button="pageup">Page Up</div></th></tr>
    <tr class="XSLGE_colhead">
     <xsl:for-each select="cols/col">
      <th colref="{@name}"><xsl:value-of select="@text"/></th>
     </xsl:for-each>
    </tr>
   </thead>
   <tbody>
    <tr>
     <xsl:for-each select="cols/col">
      <td colref="{@name}"/>
     </xsl:for-each>    
    </tr>
   </tbody>
   <tfoot>
    <tr><th colspan="{count(cols/col)}"><div button="pagedown">Page Down</div></th></tr>
   </tfoot>
  </table>
 </xsl:template>
</xsl:stylesheet> 