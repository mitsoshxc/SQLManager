using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace SQLManager.Controllers
{
    public class TableController : Controller
    {
        public async Task<IActionResult> Index(string Name)
        {
            var _Data = new DataTable();
            List<string> _PrimaryKey = new List<string>();

            if (Extensions.Connection[0] == null)
            {
                TempData["Error"] = "Seems like you are not connected to a database yet.";
                return RedirectToAction("Index", "Home");
            }

            if (Extensions.Connection[0].Equals("SQLServer"))
            {
                using (var _conn = new SqlConnection(Extensions.Connection[1]))
                {
                    await _conn.OpenAsync();
                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"   SELECT COLUMN_NAME
                                                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                                                    WHERE OBJECTPROPERTY(
                                                        OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)),
                                                        'IsPrimaryKey'
                                                    ) = 1
                                                    AND TABLE_NAME = '" + Name + "'";

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            while (await _reader.ReadAsync())
                            {
                                _PrimaryKey.Add(_reader.GetString(0));
                            }
                        }
                    }

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"   SELECT COLUMN_NAME
                                                    FROM INFORMATION_SCHEMA.COLUMNS
                                                    WHERE COLUMNPROPERTY(
                                                        OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME),
                                                        COLUMN_NAME,
                                                        'IsIdentity'
                                                    ) = 1
                                                    AND TABLE_NAME = '" + Name + "'";

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            while (await _reader.ReadAsync())
                            {
                                ViewBag.AutoInc = _reader.GetString(0);
                            }
                        }
                    }

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"SELECT * FROM " + Name;

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            _Data.Load(_reader);
                        }
                    }
                }
            }
            else if (Extensions.Connection[0].Equals("SQLite"))
            {
                using (var _conn = new SqliteConnection(Extensions.Connection[1]))
                {
                    await _conn.OpenAsync();
                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"PRAGMA table_info('" + Name + "');";

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            while (await _reader.ReadAsync())
                            {
                                _Data.Columns.Add(_reader.GetString(1));

                                if (_reader.GetInt32(5) == 1)
                                {
                                    _PrimaryKey.Add(_reader.GetString(1));
                                }
                            }
                        }
                    }

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"SELECT * FROM " + Name;

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            var _ColumnData = new string[_reader.FieldCount];

                            while (await _reader.ReadAsync())
                            {
                                for (int i = 0; i < _reader.FieldCount; i++)
                                {
                                    _ColumnData[i] = _reader[i].ToString();
                                }

                                _Data.Rows.Add(_ColumnData);
                            }
                        }
                    }

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"SELECT * FROM sqlite_sequence WHERE name = '" + Name + "'";

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            if (_reader.HasRows && _PrimaryKey.Count > 0)
                            {
                                ViewBag.AutoInc = _PrimaryKey[0];
                            }
                        }
                    }
                }
            }
            else if (Extensions.Connection[0].Equals("MySQL"))
            {
                using (var _conn = new MySqlConnection(Extensions.Connection[1]))
                {
                    await _conn.OpenAsync();

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"SELECT T0.COLUMN_NAME
                                                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE T0
                                                WHERE
                                                    T0.TABLE_SCHEMA = '" + Extensions.Connection[1].Split('=').Last() + @"'
                                                    AND TABLE_NAME = '" + Name + @"'
                                                    AND T0.CONSTRAINT_NAME='PRIMARY'
                                                ORDER BY TABLE_NAME";
                        
                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            while (await _reader.ReadAsync())
                            {
                                _PrimaryKey.Add(_reader.GetString(0));
                            }
                        }
                    }

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"SELECT COLUMN_NAME
                                                FROM INFORMATION_SCHEMA.COLUMNS
                                                WHERE
                                                    TABLE_SCHEMA = '" + Extensions.Connection[1].Split('=').Last() + @"'
                                                    AND TABLE_NAME = '" + Name + @"'
                                                    AND EXTRA = 'AUTO_INCREMENT'";
                        
                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            while (await _reader.ReadAsync())
                            {
                                ViewBag.AutoInc = _reader.GetString(0);
                            }
                        }
                    }

                    using (var _Command = _conn.CreateCommand())
                    {
                        _Command.CommandText = @"SELECT * FROM " + Name;

                        using (var _reader = await _Command.ExecuteReaderAsync())
                        {
                            _Data.Load(_reader);
                        }
                    }
                }
            }

            ViewBag.Title = "View " + Name + " data";
            ViewBag.Table = Name;
            ViewBag.PrimaryKey = _PrimaryKey;
            ViewBag.Database = Extensions.Connection[1].Split('=').Last();
            ViewBag.Type = Extensions.Connection[0];

            return View(_Data);
        }
    }
}