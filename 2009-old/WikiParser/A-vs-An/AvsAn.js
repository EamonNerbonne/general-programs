﻿var trie = (function () {
	"use strict";
	var dict = "[1990f71:4ea361]08[8:19]09[5:11]0-11[6:c]0-4[76:79]0-4 [4d:20]0-6-[2b:48]0-8[13:24]11[c0c:139a]110[1ff:35]111[7a:2c]112[bf:12]113[5a:d]114[77:f]115[d0:24]116[76:d]117[72:f]118[72:d]119[56:6]11.4[12:d]18[9b2:2b6f]180[2e2:11b]1800[24:5f]1801[4:16]1802[3:b]1803[3:13]1804[2:22]1805[5:10]1806[1:f]1807[0:c]1808[0:10]1809[2:15]181-[b:0]181 [b:0]182-[15:0]182 [f:1]183-[d:0]183 [9:2]184-[12:0]184 [b:3]185-[17:2]185 [1b:0]186-[b:0]186 [b:3]187-[f:0]187 [d:0]188-[14:0]188 [a:3]189-[c:0]189 [c:4]8[ae8:3bbf]800x[1d:f]890[11:a]8,1[26:7]8,2[11:a]–i[1:9]#i[8:14]$11 [9:33]$11.[10:18]$18 [8:34]$18,[4:9]$18.[7:d]$8[91:22b]&a[5:c]&o[4:b]*a[2:12]*e[1:10]*i[0:b].av[a:f].mp[4:a].og[d:16]/a[4:e]/e[3:9]/h[8:e]/i[1:c]/l[3:9]/s/[0:d]@[17:30]`a[1:a]£8[28:7c]∞[2:b]a[4a5c:198be7]a [984:513]abou[145:ea]about-[0:35]agai[8a:4b]al-I[4f:2]algu[8:2]alth[f:9]amon[17:12]an [1f7:166]and[1285:9dd]and\"[0:34]anda[0:f]ande[2:27]andr[5:2f3]anot[8f:68]anyw[1b:7]apart [9:4]appears[16:4]apre[8:3]are [97:76]are:[24:2]artí[a:0]A[19ba:509aa]A$[34:a]AAA[b2:88]Akiz[9:2]Amar\"[a:0]Andaluc[11:3]Armat[9:3]Asturias[9:1]Athl[b4:a8]Athleti[1:59]Athlo[1:21]AU$[4c:18]AUD[25:f]AUSC[c:6]Á[4:f]á[6:d]à[3:17]Ä[1:a]ā[4:17]Å[1:11]æ[1:b]Æ[10:11]Æn[e:0]Bhai[8:26]Bhá[0:22]Buddhism[11:18]contains[e:16]Chais[2:e]County,[5:25]das [0:26]dem [1:13]der [0:a12]describes[1:d]Dinas[0:c]Diver-[5:a]Dún[9:4]e[25d2:fb096]e.g[d:7]each [f:a]either [35:24]either.[c:2]el-[9:3]ella[b:5]empez[b:0]enoug[1c:a]eu[985:d2]eup [0:b]ew[34:e]exists[11:5]E[343f:1b38c]Empez[a:0]Enam[c:2]Esp[2da:ef]Espad[2:c]Espe[6:bd]Espo[0:c]Eu[28c0:1c8]Eul[19:80]EUR[32:3]é[8e:112]ég[b:0]ét[72:38]éta[2:26]étu[1:c]É[3:2a]f-[c:32]f [7:19]f\"[2:10]f/[4:18]fM[2:1f]fp[3:c]ft[c:15]F0[2:3b]F1[27:14c]F2[11:ba]F3[13:8d]F4[11:c3]F5[a:90]F6[9:15]F9[5:a]F'[4:15]F-[aa:3db]F [16:ed]F\"[9:7c]F#[8:15]F,[0:1e]F.[36:98]F/[f:2f]F”[0:b]FA[d92:12b0]FAC[2c4:21c]FAD[17:4]FAIR[a:0]FAL[d:2]FAM[30:0]FAN[1a:0]FAP[16:0]FAQ[d6:70]FAR[f8:52]FAS[21:9]FAT[3f:2]FB[5d:500]FC[82:18b]Fc[1:e]FD[20:e0]FEC[2:9]FEI[1:9]FF[40:76]FF [16:7]Ff[3:9]Fh[1:4a]FH[f:1f]FIA[2c:5a]FIAT[c:1]FID [5:a]FIR [a:30]FIS [10:1e]FK[6:e]FLC[34:41]FLN[1:a]FLP[2:c]FM[a7:503]FMR[9:2]FO [0:e]FOI [4:d]FP[2c1:334]FP.[d2:84]FP?[e:5]FPC?[8:2]FRC[7:d]FRS[8:16]FS[48:c2]FT[f5:52b]FTS[22:4]FTT[10:9]FU [31:3f]FU,[1:b]FU.[1:14]FV[6:c]FW[1a:23]FWD[9:3]FX[a:26]FY[11:89]Fσ[1:e]Ghae[0:d]Ghai[0:c]h'[8:13]h-[1b:8e]h-U[15:0]h [5:4e]h\"[6:25]h,[0:c]hC[1:23]heir[42:6f5]heira[26:1]hims[5:d]historic\"[f:10]historic\" [e:9]homa[445:5ea]homm[f:23]hon[4f5:4a57]honey[2ee:0]honk[65:0]honv[a:0]hors [3:1c]hour[62:48fd]ht[3e:62]htt[20:1f]http [6:10]H1[c:20]H2[13:2c]H3[5:e]H4[2:9]H5[5:28]H'[5:c]H-[36:11b]H [17:8b]H\"[d:3f]H&[a:16]H,[0:e]H.[40:64]H.A[b:0]H+[9:f]Habilitations[3:9]HB[3d:139]HC[20:41]HD[100:257]HDB[f:a]Heir[2:15]HF[1e:47]HG[e:15]HH[19:4a]HI[b0:179]HID[16:a]HIG[1b:3]HIM[9:2]HIP[12:2]HL[2e:42]HLA-D[f:8]HM[45:92]HN[17:39]Hon[9d3:1097]Hond[32d:1c]Hone[53:25]Hones[2:25]Hong[598:4]Honk[f:0]Honol[28:1]Hour[1:4f]HO [6:f]HOV[b:2a]HP[57:fa]HQ[1a:39]HR[46:67]HRT[15:8]HS[68:93]HS [17:8]HSR[18:7]HST[e:7]HT[af:30d]HTP[e:3]HV[29:48]HWT[2:8]i[281d:cc41e]i.e[11:3]ibn[11:0]if [da:6c]ii[f:a]includi[11:b]indicates[9:4]instea[44:40]instead?[5:29]is [781:24c]is.[f:3]it [55:3f]iu[13:8]I[f1d:1df7f]I-A[1d:5]I-I[9:1]III[1e:d]Ilb[20:1]IMH[8:3]Imams[1c:0]IR£[8:2]Islam [ab:1f]Islam,[48:0]Islam.[4f:0]Islands[19:4]İ[1:a]Jia[17:37]Jian[15:0]ku [3:17]l [8:12]l\"[2:14]lp[3:8]L1[b:2d]L2[f:4e]L3[5:2a]L5[7:c]L'[24:51]L'A[d:0]L-[74:1f8]L-a[e:9]L [11:84]L\"[5:8f]L&[7:c]L,[1:d]L.[2d:c9]L/[8:11]Lae[8:11]Laoig[0:e]LA [24:93]LAL[6:e]LAP[13:47]LB[1b:40]LC[87:296]LD[6e:212]LE[159:16a]LEA[29:1b]LEE[62:5]LEG[31:1]LEO[1b:7]LEP[b:3]LET[d:5]LF[11:61]LG[b3:172]LH[17:27]LIR[9:e]LL[ec:839]LM[4a:c7]LMX[a:0]LN[22:6b]Locha[4:20]LOE[6:c]LP[7f:4f6]LR[24:b3]LS[48:17c]LT[37:ac]LU [3:c]LV[12:3b]LX[3:b]LZ[9:12]m-[9:63]m [14:35]m\"[3:17]m&[4:19]m×[1:f]makes [8:12]mb[8:17]mein[1:f]mentions[4:1f]mf[4:b]mp[1c:bb]mR[16:4a]mt[9:16]M1[66:176]M19[24:1d]M190[0:b]M2[36:7a]M3[11:3d]M4[25:6b]M5[a:1b]M6[16:53]M7[13:1f]M8[4:1b]M9[3:f]M'[e:1a]M-[6f:131]M-t[29:1a]M [2a:87]M\"[d:45]M&[9:27]M,[0:e]M.[4e0:1042]M.A.S[b:0]M/[2:19]MA[475:81b]MAC[ed:1]MAD[31:3]MAF[11:3]MAG[17:2]MAJ[54:0]MAL[23:1]MAM[c:1]MAN[3b:3]MAP[2f:3]MAR[3c:1]MAS[45:5]MAT[29:4]MAX[2b:0]MAY[a:0]MB[ce:9c8]MC[6b:232]MD[6d:21c]Me-[4:b]MEd[4:9]MEn[1:e]MEP[20:d9]MF[70:349]Mf[34:14d]MG[3c:103]MH[33:d0]Mh[8:28]Mie[9:e]MI5[1:2b]MI6[7:45]MI [2:e]MIA[7:d]MIT[1e:ad]MK[25:56]ML[d4:305]MM[11d:239]MMT[15:6]MN[1b:6b]MoU[12:1a]MO [7:f]MOT [6:c]MOU[1b:26]MP[11e:bed]MR[4a:1dd]MS[180:54d]Msc[1:a]MT[f5:386]MTR[30:22]MUV[2:c]MV[4b:ed]MX[e:45]N4[2:a]N6[9:15]N'[a:f]N-[99:156]N-a[2a:15]N-S[e:5]N [21:62]N\"[9:2f]N,[5:a]N.[30:39]N.Y[8:2]N=[1:9]N²[0:c]Nao[4:e]NA [b:13]NAA[3a:8f]NAAF[b:0]NAI[22:5d]NASL[4:15]NB[d9:875]NC[1b6:714]ND[30:120]NEA[12:4e]NEH[3:16]NES [35:43]NF[b7:91b]NG[6a:26b]NH[ae:5ae]NI[cd:f1]NIC[26:a]NIL[a:2]NIM[e:c]NIMH[4:c]NIN[f:0]NIS[1c:e]NJC[f:16]NK[1b:62]NL[2d:e9]NLS[9:4]NM[21:d5]NNR[0:a]NNT[4:b]NP[841:8a6]NPO[798:5db]NPOV-[e:17]NR[82:f6]NRJ[a:1]NRT[2d:d]NS[e0:19c]NSW[6f:9]NT[45:d6]NT$[b:1]NUS[1:f]NV[24:59]Nv[6:15]NWA[14:4a]NX[4:29]NYP[1e:68]NYU[7:30]n-[63:3b7]n−[3:b]n [5b:b4]n\"[5:20]n&[a:41]n,[2:10]n+[4:1d]n×[4:22]nda[7:1b]npa[6:b]nt[10:3c]nV[7:17]nW[2:d]o[c81d:760be]obr[9:2]occurs[9:3]ocho[8:2]of [812:186]on[acdb:7ba8]on-[4e:135d]on/[2:68]onb[0:141]onco[1:280]ond[0:c]oner[4:40]ong[47:1efa]oni[5:124]onl[113:389c]onm[2:29]ono[1:89]onr[1:27]ons[7:345]ont[4:1ba]onu[2:1e]onw[1:24]ony[1:1c]or [519:164]or,[b:1]oui[1f:6]O[83f:9a45]Obers[72:59]Oberst [3:1d]Oberstl[3:b]Olv[d:0]One[27d:2d]Onei[0:21]ONE[12:2]Oop[b:1]Oui[54:7]Ó[1:11]Ö[a:1d]ö[1:a]Ō[1:16]ō[4:1f]Phob[6:1d]Phoi[0:c]r'[1:f]r-[7:26]r [12:2a]r\"[1:20]r&[2:a]r.[6:d]refers[6:19]rf[22:96]rm[3:8]rs[e:13]R1[25:50]R10[a:5]R2[e:1e]R3[a:1e]R4[7:1c]R5[6:b]R6[3:9]R'[1:1d]R-[43:1bf]R [24:152]R\"[9:54]R&[100:341]R,[1:10]R.[47:6c]R.C[b:2]R/[4:14]RA [6:17]RAF[98:2a4]RB[4e:2b9]RC[5a:1f2]RD[3d:c5]RE [2:f]RER[5:c]RF[2c7:e5b]Rf[10bd:2960]RG[14:95]RHS[1:b]RIA[14:54]RIC [7:1c]RJ[5:38]RK[b:8f]RL[13:2e]RL [e:8]RM[69:99]RM1[10:4]RN[60:239]RNG[8:2]ROT[20:48]RP[86:285]RQ[4:e]RR[1a:5f]RS[3ba:441]RS [1b1:12f]RS)[1d:a]RS,[78:4f]RS.[aa:61]RS?[2a:e]RST[1a:3]RT[6a:187]RU[2f:3b]RV[1c:b3]RX[9:11]s-[a:3f]s\"[b:65]s)[2:b]s,[1:14]s.[e:19]says[a:25]sich[4:20]sp3[2:d]sprot[4:a]ssh[5:a]states [11:34]states:[0:e]sv[52:5e]sva[b:0]sve[17:0]S1[e:24]S2[8:f]S3[7:13]S4[4:d]S5[2:9]S6[4:c]S'[4:2f]S-[76:231]S [18:10e]S\"[f:88]S&[28:69]S&W[a:5]S,[2:15]S.B[a:1c]S.M[8:11]S.O[4:18]S”[1:9]SA-[27:49]SA-1[8:3]SA [19:64]SACD[9:11]SAE[8:e]SAS[38:47]SASE[a:2]SAT [19:2c]SATB[4:b]SB[42:a2]SCA [5:14]SCC[e:25]SCM[3:9]SCO [2:8]SCR[17:1d]SCRA[d:0]SCT[4:17]SD[77:20e]SE [12:18]SEC[47:e9]SECO[d:0]SECR[e:3]SEI[5:b]SEO[1e:37]SF[69:af]SG[35:92]SH2[1:a]SH3[1:a]SH-[b:11]SI [a:6c]SJ[5:a]SK[25:3d]SL[8d:e9]SLA[2c:c]SLI[14:d]SLO[10:5]SM[c8:1f0]SMA[32:a]SME[22:13]SME [2:f]SMI[d:7]SN[fd:126]SNA[15:6]SNE[25:e]SNO[3d:0]SO([5:f]SOA [c:20]SOAI[0:d]SOE[5:1e]SOI[2:8]SOS[15:a6]SOV[7:12]SP[3cb:508]SPAC[c:0]SPAD[19:0]SPAM[12:0]SPAN[d:0]SPAR[3a:0]SPE[4d:d]SPE [1:b]SPIC[d:0]SPO[1c:d]SPU[10:3]SR[50:dc]SS[13e:4d1]ST-[1:b]STA [0:14]STB[4:9]STC[2:15]STD[d:54]STF[2:a]STL[3:12]STM[a:10]STS[4:18]STV[6:1c]Sura [5:a]SU[d9:15e]SUB[e:0]SUL[48:22]SUN[1b:0]SUP[10:1]SUS[a:0]SV[93:2ba]SWF[b:12]SWP[2:a]SWR[2:f]SX[13:16]SXS[a:1]t-S[1:48]than [41:18a]tS[1:41]Taves[0:10]Tà[0:b]u[20706:3d585]u-[55:3]u [2f:4]u\"[2c:2]u.[12:1]ub[164:4f]ube[6:2a]uf[10:5]uk[86:39]uka[7:24]ulu[1c:e]um [a:1]un [92:10]una [28:5]unan[8d3:1ae]unana[2:9]unann[2:dd]unans[0:5b]unant[0:3d]unary[83:7]une [16:0]uni[11215:5e88]unicorp[0:d]unid[62:692]unidi[5b:9]unim[2b:272]unimo[22:1]unin[cd:5376]unintentionall[27:1b]unintentionally [0:18]univo[b:1a]unles[14:6]until [16:10]upo[a:4]ura[13d:12]ure[58:e]uri[197:a]url[79:1e]uro[63:f]us[d0d7:3fd]us-[4:c]us [6:34]ush[1:cb]ut[9d9:469]utm[0:14]utt[8:3e2]uv[4b:f]uw[51:9]U1[20:29]U-21[9:e]U-23 [6:10]U-B[4a:4c]U-Bo[3d:0]Ua[1:5b]Ub[24:57]Ubi[e:7]UDP-[7:1e]Ud[4:15]Ugl[0:31]Uh[6:12]Ui[e:18]Ul[39:35e]Uli[e:4]Um[16:b6]UMN[7:10]Un-[1:c]Una[21:31]Unan[11:7]Unb[1:30]Unc[8:de]Und[14:282]Une[20:25]Unes[1b:0]Unf[1:1e]Ung[0:c]Unh[3:e]Unid[3:13]Unin[4:13]Unk[2:7b]Unl[4:34]Unm[2:20]Unn[2:d]Uno[5:1d]Unp[1:22]Unr[7:64]Uns[3:43]Unt[2a:50]Unters[26:f]Unu[1:15]Unw[1:13]Up[19:3bb]Ur[348:590]Ura[5b:17]Uri[f:7]Uru[237:11c]Uruguayan-[8:d]Uruk[1:e]Ush[2:f]Ust[7:28]Utn[1:a]Uto-[9:11]Utr[5:1c]Utt[3:e]Ux[2:f]Uz[38:f9]ü[5:15]Ü[2:1a]verses[0:c]Valley,[1:a]VII[13:19]WikiEl[3:8]x[2c0:2f2]xa[2d:1]xe[11b:1]xi[2f:2]xo[f:2]xx[22:a]xy[83:b]X[446:13a4]Xa[4a:2]XA[12:6]Xe[b6:1]Xh[25:0]Xi[c4:17]Xiany[0:a]XIV[8:3]XIX[a:2]Xo[d:2]Xu[1f:0]XU[9:3]XV[23:b]XX[2d:2b]XX [6:d]Xy[14:0]Yp[2:8]Zen)[0:a]α[23:f2]ε[1:1e]ω[6:22]西[0:e]長[0:c]";

	var trie = function () {
		var root = {};
		var prefLines = dict.replace(/([^\[]*)\[([0-9a-f]+):([0-9a-f]+)\]/g, function (m, prefix, aCount, anCount) {
			var node = root;
			while (prefix) {
				node = node[prefix[0]] || (node[prefix[0]] = {});
				prefix = prefix.substring(1);
			}
			node.data = { aCount: parseInt(aCount, 16), anCount: parseInt(anCount, 16) };
		});
		return root;
	}();

	function findTrieNode(word) {
		var suffix = word, node = trie, data;
		while (true) {
			data = node.data || data;
			if (!suffix) break;
			node = node[suffix[0]];
			if (!node) break;
			suffix = suffix.substring(1);
		}
		return { prefix: word.substring(0, word.length - suffix.length), data: data };
	}

	var articleEl = document.getElementById("article");
	var articleDetailEl = document.getElementById("articleDetail");
	var inputEl = document.getElementById("searchquery");

	inputEl.onkeyup = inputEl.oninput = function () {
		var input = document.getElementById("searchquery").value.replace(/^\s+|\s+$/g, "") + " ";
		input = input.replace(/^[\(\"'“‘-]/, ""); //strip initial punctuation symbols
		var node = findTrieNode(input);
		var article = node.data.aCount > node.data.anCount ? "a" : "an";
		articleEl.replaceChild(document.createTextNode(article), articleEl.firstChild);
		articleDetailEl.replaceChild(document.createTextNode(node.prefix + "[" + article + ":" + node.data.anCount + ":" + node.data.aCount + "]"), articleDetailEl.firstChild);
	}

	return trie;
})();

function iterTrie(func) {
	function impl(prefix, node) {
		func(prefix, node);
		for (var c in node)
			if (c.length === 1)
				impl(prefix + c, node[c]);
	}
	impl("", trie);
}