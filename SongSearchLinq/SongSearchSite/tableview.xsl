<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output doctype-public="html" />
	<xsl:template match="/">
		<html>
			<head>
				<style type="text/css">
					body,html{
					width:100%;
					padding:0;
					margin:0;
					position:relative;
					}
					table {
					border-collapse:collapse;
					width:100%;
					}
					tr{
					border: solid #8ba;
					border-width: 1px 0;
					}

					body {
					font-family:  Segoe UI,Calibri,Helvetica,Arial, Sans-Serif;
					font-size:10pt;
					}
					tr:nth-child(2n) {
					background: #e2eeea;
					}
					tr > *:nth-child(2) {
					font-style:italic;
					}
					td>div {
					overflow:hidden;
					max-height: 1.28em;
					max-width:100%;
					position:relative;
					}
					td>div>span {
					position:relative;
					z-index:1;
					}

					tr td>div>span {
					background:#fff;
					}

					tr:nth-child(2n) td>div>span {
					background:#e2eeea;
					}

					td>div>i {
					float:right;
					position:relative;
					white-space:nowrap;
					width:0;
					overflow:show;
					background:red;
					z-index:0;
					}
					td>div>i>i {
					position:absolute;
					bottom:0.0ex;
					font-weight:bold;
					right:0;
					color:#aaa;
					}

				</style>
				<!--#8ba;#c4ddd5;#e2eeea-->
			</head>
			<body>
				<table>
					<colgroup>
						<col/>
						<col />
						<col align="right" style="width:2em; text-align:right;" />
						<col  align="char" char=":" style="width:3em"/>
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
					<tbody>
					<xsl:apply-templates select="*"/>
					</tbody>
				</table>
			</body>
		</html>
	</xsl:template>

	<xsl:template match="songs">
		<xsl:apply-templates select="*"/>
	</xsl:template>

	<xsl:template match="partsong|songref|song">
		<xsl:variable name="songlabel">
			<xsl:apply-templates select="." mode="makelabel" />
		</xsl:variable>
		<tr onclick="parent.BatPop(this.getAttribute('songlabel'),decodeURIComponent(this.getAttribute('href').substring(this.getAttribute('href').lastIndexOf('/')+1)), this.getAttribute('href'), null);" style="cursor:pointer;" href="{@songuri}" songlabel="{$songlabel}">
			<!-- onclick="top.frames['idxStatus'].location.href='{@songuri}'" -->
			<xsl:choose>
				<xsl:when test="@artist">
					<td>
						<div>
							<span>
								<xsl:value-of select="@artist"/>
							</span>
							<i><i>............ ?<br/>............ ?<br/>............ ?</i></i>
						</div>
					</td>
					<td>
						<div>
							<span>
								<xsl:value-of select="@title"/>
							</span>
							<i><i>............ ?<br/>............ ?<br/>............ ?</i></i>
						</div>
					</td>
					<td>
						<div style="text-align:right;margin-right:0.5em;">
							<span>
								<xsl:value-of select="@track"/>
							</span>
							<i><i>............ ?<br/>............ ?<br/>............ ?</i></i>
						</div>
					</td>
					<td>
						<div style="text-align:right;margin-right:0.5em;">
							<span>
								<xsl:value-of select="concat(number(floor(number(@length) div 60)),':',substring('0',floor(number(@length) mod 60 div 10) +1), string(number(@length) mod 60))"/>
							</span>
							<i><i>............ ?<br/>............ ?<br/>............ ?</i></i>
						</div>
					</td>
					<td>
						<div>
							<span>
								<xsl:value-of select="@album"/>
							</span>
							<i><i>............ ?<br/>............ ?<br/>............ ?</i></i>
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