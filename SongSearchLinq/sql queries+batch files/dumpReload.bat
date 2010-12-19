echo pragma page_size=4096; > lastFMcache.dump
echo pragma cache_size=350000; >> lastFMcache.dump
echo pragma synchronous=0; >> lastFMcache.dump
sqlite3 lastFMcache.s3db .dump >> lastFMcache.dump
move lastFMcache.s3db lastFMcache.old.s3db
move lastFMcache.s3db-journal lastFMcache.old.s3db-journal
sqlite3 lastFMcache.s3db < lastFMcache.dump
