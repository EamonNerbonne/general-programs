echo pragma page_size=4096; > D:\EamonLargeDocs\lastFMcache.dump
echo pragma cache_size=350000; >> D:\EamonLargeDocs\lastFMcache.dump
echo pragma synchronous=0; >> D:\EamonLargeDocs\lastFMcache.dump
"C:\Program Files (Custom)\sqlite3\sqlite3.exe" lastFMcache.s3db .dump >> D:\EamonLargeDocs\lastFMcache.dump
