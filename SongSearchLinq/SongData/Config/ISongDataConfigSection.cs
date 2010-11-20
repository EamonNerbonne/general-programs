
namespace SongDataLib
{
	public interface ISongDataConfigSection
	{
		void Load(SongDataLoadDelegate handler);
		void RescanAndSave(FileKnownFilter filter, SongDataLoadDelegate handler);
	}
}
