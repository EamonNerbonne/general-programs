<Query Kind="Statements">
  <Reference Relative="..\EmnExtensions\bin\Release\EmnExtensions.dll">E:\emn\programs\EmnExtensions\bin\Release\EmnExtensions.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>EmnExtensions</Namespace>
</Query>

var table = 
File.ReadAllLines(Path.GetDirectoryName (Util.CurrentQueryPath) +@"\dominion - Card Names (translated).csv")
.Select(line=> line.Split(','))
.ToArray()
;

var colCount=table.Max(line=>line.Length);
var cols = 
	Enumerable.Range(0,colCount)
	.Select(ci=> 
		table
			.Select(row=>row.Skip(ci).FirstOrDefault())
			.Reverse().SkipWhile(s=>string.IsNullOrWhiteSpace(s)).Reverse()
			.ToArray())
	.ToArray();
	
var badCols = new[]{ "Basic Cards", "Dark Ages", "Seaside", "Guilds" ,"Promo"};
var extraCards = new[]{"stash"};

var englishCols = 
	cols.Where(col=>col[1]=="English")
	.Select(col=>col.Take(1).Concat(col.Skip(2)).ToArray())
	.ToArray();
var setsIHave = englishCols.Where(col=>!badCols.Contains(col[0])).ToArray();
setsIHave.Select(col=>col.First()).Dump();

setsIHave.SelectMany(set=>set.Skip(1).Select(card=>new { card, set=set.First() })).OrderBy(c=>c.card) .Dump();
