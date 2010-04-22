using System;

namespace SongDataLib
{
	public class SongDatabaseConfigException : Exception
	{
		public SongDatabaseConfigException(SongDatabaseConfigFile databaseConfigFile, string message) : base("Error while parsing " + databaseConfigFile.configPathReadable + ":\n" + message) { }
		public SongDatabaseConfigException(SongDatabaseConfigFile databaseConfigFile, Exception innerException) : base("Error while parsing " + databaseConfigFile.configPathReadable + ".", innerException) { }
	}
}
