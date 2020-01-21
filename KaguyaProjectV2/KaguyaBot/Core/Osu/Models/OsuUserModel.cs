﻿using System;

namespace KaguyaProjectV2.KaguyaBot.Core.Osu.Models
{
    public class OsuUserModel : OsuBaseModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        //public int Count300 { get; set; }
        //public int count100 { get; set; }
        //public int count50 { get; set; }
        //public int playcount { get; set; }
        //public long ranked_score { get; set; }
        //public long total_score { get; set; }
        //public int pp_rank { get; set; }
        //public double level { get; set; }
        //public double pp_raw { get; set; }
        //public double accuracy { get; set; }
        //public int count_rank_sh { get; set; }
        //public int count_rank_ssh { get; set; }
        //public int count_rank_ss { get; set; }
        //public int count_rank_s { get; set; }
        //public int count_rank_a { get; set; }
        public string Country { get; set; }
        //public int pp_country_rank { get; set; }
        //public int total_seconds_played { get; set; }
        public DateTime JoinDate { get; set; }
        public TimeSpan Difference { get; set; }
    }
}
