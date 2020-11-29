using ExifLibrary;
using System;
using System.IO;
using System.Linq;

namespace FixPhotoDates
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Starting processing of {args[0]}");

            //foreach (var file in Directory.EnumerateFiles(args[0], "*.jpg"))
            //{
            //    var imageFile = ImageFile.FromFile(file);
            //    var exifDateOriginal = imageFile.Properties.Get<ExifDateTime>(ExifTag.DateTimeOriginal);
            //    if (exifDateOriginal.Value.Year == 2009)
            //    {
            //        var newDate = exifDateOriginal.Value.AddYears(1);
            //        imageFile.Properties.Set(ExifTag.DateTimeOriginal, newDate);
            //        imageFile.Save(file);
            //    }
            //}

            //return;

            //var imageFile = ImageFile.FromFile(args[0]);
            //imageFile.Properties.Set(ExifTag.DateTimeOriginal, new DateTime(2010, 4, 7));
            //imageFile.Save(args[1]);

            ProcessFolder(args[0]);

            foreach (var dir in Directory.EnumerateDirectories(args[0], "*", SearchOption.AllDirectories))
            {
                ProcessFolder(dir);
            }

            Console.WriteLine($"Done processing {args[0]}");
        }

        static void ProcessFolder(string dir)
        {
            Console.WriteLine(dir);
            int year = 0, month = 0, day = 0;

            var parts = dir.Split(Path.DirectorySeparatorChar);
            foreach (var segment in parts)
            {
                if (segment.Length >= 4 &&
                    Int32.TryParse(segment.Substring(0, 4), out int yearTmp) &&
                    yearTmp >= 1900 && yearTmp < 2050)
                {
                    year = yearTmp;
                    continue;
                }

                if (segment.Length >= 5 && segment[2] == '-') {
                    Int32.TryParse(segment.Substring(0, 2), out int monthTmp);
                    Int32.TryParse(segment.Substring(3, 2), out int dayTmp);

                    if (monthTmp > 0 && monthTmp <= 12)
                    {
                        month = monthTmp;
                        if (dayTmp > 0 && monthTmp <= 31) day = dayTmp;
                    }
                }
                else if (year != 0 &&
                    (segment.Length == 2 || (segment.Length >= 3 && segment[2] == ' ')))
                {
                    // It looks like "03 Some text"

                    if (!Int32.TryParse(segment.Substring(0, 2), out int num))
                        continue;

                    if (month == 0)
                    {
                        if (num > 0 && num <= 12)
                            month = num;
                    }
                    else if (day == 0)
                    {
                        if (num > 0 && num <= 31)
                            day = num;
                    }
                }
            }

            if (year == 0)
            {
                Console.WriteLine($"****** No year folder in {dir}! ******");
                return;
            }

            if (month == 0) month = 6;
            if (day == 0) day = 15;
            var folderDate = new DateTime(year, month, day, 10, 0, 0);

            //Console.WriteLine($"{dir} date is {folderDate}");

            DateTime fileDate = folderDate;

            foreach (string file in Directory.GetFiles(dir).OrderBy(f => f))
            {
                // Add a second to keep a chronological order
                fileDate = fileDate.AddSeconds(1);

                switch (Path.GetExtension(file).ToLower())
                {
                    case ".jpg":
                        var imageFile = ImageFile.FromFile(file);

                        var exifDateOriginal = imageFile.Properties.Get<ExifDateTime>(ExifTag.DateTimeOriginal);
                        if (exifDateOriginal != null && exifDateOriginal.Value.Year > 1950 /*&& exifDateOriginal.Value.Year != 2000*/)
                        {
                            //Console.WriteLine($"    {file}: {exifDateOriginal.Value}");
                            fileDate = exifDateOriginal.Value;

                            // The EXIF year should always match the folder derived year
                            if (fileDate.Year != year)
                            {
                                Console.WriteLine($"    BAD FILE YEAR {file}: {fileDate}");
                                //throw new Exception($"BAD FILE YEAR {file}: {fileDate}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"    SET TIME {file}: {fileDate}");
                            imageFile.Properties.Set(ExifTag.DateTimeOriginal, fileDate);

                            // ACTION LINE!
                            //imageFile.Save(file);

                            //File.SetLastWriteTime(file, fileDate.Value);
                        }
                        break;

                    case ".mov":
                    case ".wmv":
                    case ".mpg":
                    case ".avi":
                    case ".mp3":
                        DateTime videoTime = File.GetLastWriteTime(file);

                        // If timestamp is off by more than a day
                        if ((fileDate - videoTime).Duration() > new TimeSpan(1, 0, 0, 0))
                        {
                            Console.WriteLine($"    SET VIDEO TIME {file}: {videoTime} -> {fileDate}");

                            // ACTION LINE!
                            //File.SetLastWriteTime(file, fileDate);
                        }
                        break;

                    case ".db":
                    case ".cmd":
                    case ".ion":
                    case ".txt":
                    case ".xls":
                    case ".doc":
                    case ".htm":
                    case ".msg":
                        break;

                    default:
                        //Console.WriteLine($"    UNKNOWN EXTENSION {file}");
                        break;
                }
            }

            //var latTag = imageFile.Properties.Get<GPSLatitudeLongitude>(ExifTag.GPSLatitude);

            //foreach (var prop in imageFile.Properties)
            //{
            //    Console.WriteLine($"{prop.Name} {prop.GetType()} {prop.Value}");
            //}


            //imageFile.Properties.Set(ExifTag.DateTimeOriginal, new DateTime(2010, 4, 7));
            //imageFile.Properties.Set(ExifTag.DateTime, new DateTime(2010, 4, 7));
            //imageFile.Save(args[1]);
        }
    }
}
