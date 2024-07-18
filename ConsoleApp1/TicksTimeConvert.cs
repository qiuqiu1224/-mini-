using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class TicksTimeConvert
    {
        /// <summary>
        /// 时间戳与DateTime互转
        /// </summary>

        /*
    国际标准 ISO 8601 日期与时间的规定写法：
    2000-01-23                # 年使用四位数，月、日、时、分、秒皆使用两位数，不够的前面补零。
    2000-01-23T04:05:06       # 用 T 分割日期与时间。
    2000-01-23T04:05:06Z      # 用 Z 代表 UTC+0（零时区）
    2000-01-23T04:05:06+00:00 # 也可以使用 +00:00 表示零时区
    2000-01-23T04:05:06+08:00 # 东八区
         */

        /*
         * 时间戳是没有时区概念的。或者可以理解为，时间戳是固定死零时区的.
         * 
         * 时间戳10位的是秒，13位的是毫秒
         * 
         * 1秒=1000毫秒
         * 1毫秒=1000微秒
         * 1微秒=1000纳秒，纳秒也叫毫微秒
         * 
         * DateTime.Ticks的单位是100纳秒，
         * 每个计时周期表示一百纳秒，即一千万分之一秒。
         * 此属性的值表示自 0001 年 1 月 1 日午夜 12:00:00（表示 DateTime.MinValue）以来经过的以 100 纳秒为间隔的间隔数。
         * **/


        #region 获取当前时间、时间戳
        /// <summary>
        /// 获取当前本地时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetNowLocalTime()
        {
            return DateTime.Now;
        }
        /// <summary>
        /// 获取当前UTC时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetNowUTCTime()
        {
            return DateTime.UtcNow;
        }
        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <returns>秒，10位</returns>
        public static long GetNowTicks10()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <returns>毫秒，13位</returns>
        public static long GetNowTicks13()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        #endregion

        #region UTC时间 和 时间戳 互转
        /// <summary>
        /// UTC时间转10位时间戳
        /// </summary>
        /// <param name="UTCTime"> UTC时间</param>
        /// <returns>秒，10位</returns>
        public static long UTCTime2Ticks10(DateTime UTCTime)
        {
            return new DateTimeOffset(UTCTime).ToUnixTimeSeconds();
        }
        /// <summary>
        /// UTC时间转13位时间戳
        /// </summary>
        /// <param name="UTCTime"> UTC时间</param>
        /// <returns>毫秒，13位</returns>
        public static long UTCTime2Ticks13(DateTime UTCTime)
        {
            return new DateTimeOffset(UTCTime).ToUnixTimeMilliseconds();
        }
        /// <summary>
        /// 10位时间戳转UTC时间
        /// </summary>
        /// <param name="Ticks">秒，10位</param>
        /// <returns>UTC时间</returns>
        public static DateTime Ticks102UTCTime(long Ticks)
        {
            return DateTimeOffset.FromUnixTimeSeconds(Ticks).UtcDateTime;
        }
        /// <summary>
        /// 13位时间戳转UTC时间
        /// </summary>
        /// <param name="Ticks">毫秒，13位</param>
        /// <returns>UTC时间</returns>
        public static DateTime Ticks132UTCTime(long Ticks)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(Ticks).UtcDateTime;
        }
        #endregion

        #region UTC时间 和 本地时间 互转
        /// <summary>
        /// UTC时间转本地时间
        /// </summary>
        /// <param name="UTCTime">UTC时间</param>
        /// <returns>本地时间</returns>
        public static DateTime UTCTime2LocalTime(DateTime UTCTime)
        {
            return UTCTime.ToLocalTime();
        }
        /// <summary>
        /// 本地时间转UTC时间
        /// </summary>
        /// <param name="LocalTime">本地时间</param>
        /// <returns>UTC时间</returns>
        public static DateTime LocalTime2UTCTime(DateTime LocalTime)
        {
            return LocalTime.ToUniversalTime();
        }
        #endregion

        #region 本地时间 和 时间戳 互转
        /// <summary>
        /// 本地时间转10位时间戳
        /// </summary>
        /// <param name="LocalTime">本地时间</param>
        /// <returns>秒，10位</returns>
        public static long LocalTime2Ticks10(DateTime LocalTime)
        {
            return UTCTime2Ticks10(LocalTime2UTCTime(LocalTime));
        }
        /// <summary>
        /// 本地时间转13位时间戳
        /// </summary>
        /// <param name="LocalTime">本地时间</param>
        /// <returns>毫秒，13位</returns>
        public static long LocalTime2Ticks13(DateTime LocalTime)
        {
            return UTCTime2Ticks13(LocalTime2UTCTime(LocalTime));
        }
        /// <summary>
        /// 10位时间戳转本地时间
        /// </summary>
        /// <param name="Ticks">秒，10位</param>
        /// <returns>本地时间</returns>
        public static DateTime Ticks102LocalTime(long Ticks)
        {
            return UTCTime2LocalTime(Ticks102UTCTime(Ticks));
        }
        /// <summary>
        /// 13位时间戳转本地时间
        /// </summary>
        /// <param name="Ticks">毫秒，13位</param>
        /// <returns>本地时间</returns>
        public static DateTime Ticks132LocalTime(long Ticks)
        {
            return UTCTime2LocalTime(Ticks132UTCTime(Ticks));
        }
        #endregion

        public static byte[] TimestampToBytes(long timestamp)
        {
            return BitConverter.GetBytes(timestamp);
        }

        // 将字节数组转换回时间戳
        public static long BytesToTimestamp(byte[] bytes)
        {
            return BitConverter.ToInt64(bytes, 0);
        }

    }


}

