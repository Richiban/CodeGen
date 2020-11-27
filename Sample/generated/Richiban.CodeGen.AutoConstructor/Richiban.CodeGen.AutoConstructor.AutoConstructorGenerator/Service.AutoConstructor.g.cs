namespace Sample
{
    partial class Service
    {
        public Service(System.Guid data)
        {
            if (data == null) throw new System.ArgumentNullException(nameof(data));
            this._data = data;
        }
    }
}
