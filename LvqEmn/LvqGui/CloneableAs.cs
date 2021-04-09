namespace LvqGui
{
    public abstract class CloneableAs<T> where T : CloneableAs<T>
    {
        public T Clone() { return (T)MemberwiseClone(); }
    }
}
