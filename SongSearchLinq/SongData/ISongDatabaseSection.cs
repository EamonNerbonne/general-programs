
namespace SongDataLib
{
	public interface ISongDatabaseSection
	{
		void Load(SongDataLoadDelegate handler);
		void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler);
	}
}
