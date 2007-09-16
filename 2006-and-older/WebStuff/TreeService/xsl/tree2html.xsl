<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="http://www.w3.org/1999/xhtml">
 <xsl:template match="/">
	<html><head><title>This is a static Tree test</title>	<link rel="stylesheet" type="text/css" href="../css/tree.css"/><script type="text/javascript" src="../js/simple.js"/>	</head>
	<body onload="initTree()"><div><xsl:apply-templates select="*"/></div></body></html>
</xsl:template>
<xsl:template match="node[node]">
 <div class="tree collapsed">
  <xsl:for-each select="col"><div class="treecol"><xsl:value-of select="@label"/></div></xsl:for-each>
  <div class="treehead"><span><img src="../img/imin.png" class="whenexpanded treeicon" alt="-" /><img src="../img/iplus.png" class="whencollapsed treeicon" alt="+" /></span>
   <xsl:value-of select="@label"/>
  </div>
  <xsl:apply-templates select="node"/>
 </div>
</xsl:template>
<xsl:template match="node[not(node)]">
 <div class="treeleaf">
  <xsl:for-each select="col"><div class="treecol"><xsl:value-of select="@label"/></div></xsl:for-each>
  <div class="treeleafhead"><span>* </span><xsl:value-of select="@label"/></div>
 </div>
</xsl:template>

</xsl:stylesheet>