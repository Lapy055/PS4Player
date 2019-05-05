using System;
using System.Collections.Generic;

namespace SubtitlesParser.Classes
{
    public class SubtitleItem
    {

        //Properties------------------------------------------------------------------
        
        //StartTime and EndTime times are in milliseconds
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public string Lines { get; set; }
        

        //Constructors-----------------------------------------------------------------

        ///// <summary>
        ///// The empty constructor
        ///// </summary>
        //public SubtitleItem()
        //{
        //    this.Lines = new List<string>();
        //}


        // Methods --------------------------------------------------------------------------

        //public override string ToString()
        //{
        //    //var startTs = new TimeSpan(0, 0, 0, 0, StartTime);
        //    //var endTs = new TimeSpan(0, 0, 0, 0, EndTime);

        //    //var res = string.Format("{0} --> {1}: {2}", startTs.ToString(), endTs.ToString(), string.Join(Environment.NewLine, Lines.ToArray()));

        //    string res = "";
        //    for (int i = 0; i < Lines.Count; i++)
        //        res += Lines[i];

        //    return res;
        //}

    }
}