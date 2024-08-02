﻿using SQLite;

namespace Assets.Scripts.Cache
{
    public class FileCacheEntry
    {
        [PrimaryKey, AutoIncrement]
        public int? Id { get; set; } = null;

        [Unique]
        public string Url { get; set; }

        public byte[] Data { get; set; }
    }
}