/*
 *  BeatDetektor.js
 *
 *  BeatDetektor - CubicFX Visualizer Beat Detection & Analysis Algorithm
 *  Javascript port by Charles J. Cliffe and Corban Brook
 *
 *  Created by Charles J. Cliffe <cj@cubicproductions.com> on 09-11-30.
 *  Copyright 2009 Charles J. Cliffe. All rights reserved.
 *
 *  BeatDetektor is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  BeatDetektor is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  Visit www.cubicvr.org for BeatDetektor forum and support.
 *
 */


/* 
 BeatDetektor class


 Theory:

 Trigger detection is performed using a trail of moving averages, 
 
 The FFT input is broken up into 128 ranges and averaged, each range has two moving 
 averages that tail each other at a rate of (1.0 / BD_DETECTION_RATE) seconds.  

 Each time the moving average for a range exceeds it's own tailing average by:

 (moving_average[range] * BD_DETECTION_FACTOR >= moving_average[range])

 if this is true there's a rising edge and a detection is flagged for that range. 
 Next a trigger gap test is performed between rising edges and timestamp recorded. 

 If the gap is larger than our BPM window (in seconds) then we can discard it and
 reset the timestamp for a new detection -- but only after checking to see if it's a 
 reasonable match for 2* the current detection in case it's only triggered every
 other beat. Gaps that are lower than the BPM window are ignored and the last 
 timestamp will not be reset.  

 Gaps that are within a reasonable window are run through a quality stage to determine 
 how 'close' they are to that channel's current prediction and are incremented or 
 decremented by a weighted value depending on accuracy. Repeated hits of low accuracy 
 will still move a value towards erroneous detection but it's quality will be lowered 
 and will not be eligible for the gap time quality draft.
 
 Once quality has been assigned ranges are reviewed for good match candidates and if 
 BD_MINIMUM_CONTRIBUTIONS or more ranges achieve a decent ratio (with a factor of 
 BD_QUALITY_TOLERANCE) of contribution to the overall quality we take them into the 
 contest Round.  Note that the contest Round  won't run on a given process() call if 
 the total quality achieved does not meet or exceed BD_QUALITY_TOLERANCE.
  
 Each time through if a select draft of BPM ranges has achieved a reasonable quality 
 above others it's awarded a value in the BPM contest.  The BPM contest is a hash 
 array indexed by an integer BPM value, each draft winner is awarded BD_QUALITY_REWARD.

 Finally the BPM contest is examined to determine a leader and all contest entries 
 are normalized to a total value of BD_FINISH_LINE, whichever range is closest to 
 BD_FINISH_LINE at any given point is considered to be the best guess however waiting 
 until a minimum contest winning value of about 20.0-25.0 will provide more accurate 
 results.  Note that the 20-25 rule may vary with lower and higher input ranges. 
 A winning value that exceeds 40 or hovers aRound 60 (the finish line) is pretty much
 a guaranteed match.


 Configuration Kernel Notes:

 The majority of the ratios and values have been reverse-engineered from my own  
 observation and visualization of information from various aspects of the detection 
 triggers; so not all parameters have a perfect definition nor perhaps the best value yet.
 However despite this it performs very well; I had expected several more layers 
 before a reasonable detection would be achieved. Comments for these parameters will be 
 updated as analysis of their direct effect is explored.


 Input Restrictions:

 bpm_maximum must be within the range of (bpm_minimum*2)-1
 i.e. minimum of 50 must have a maximum of 99 because 50*2 = 100


 Changelog: 
 
 01/17/2010 - Charles J. Cliffe 
  - Tested and tweaked default kernel values for tighter detection
  - Added BeatDetektor.config_48_95, BeatDetektor.config_90_179 and BeatDetektor.config_150_280 for more refined detection ranges
  - Updated unit test to include new range config example

02/21/2010 - Charles J. Cliffe 
 - Fixed numerous bugs and divide by 0 on 1% match causing poor accuracy
 - Re-worked the quality calulations, accuracy improved 8-10x
 - Primary value is now a fractional reading (*10, just divide by 10), added win_bpm_int_lo for integral readings
 - Added feedback loop for current_bpm to help back-up low quality channels
 - Unified range configs, now single default should be fine
 - Extended quality reward 'funnel'

*/

#pragma strict

	
class BeatDetektorJS 	
{

	var BD_DETECTION_RANGES:int = 128;  // How many ranges to quantize the FFT into
	var BD_DETECTION_RATE:float = 12.0;   // Rate in 1.0 / BD_DETECTION_RATE seconds
	var BD_DETECTION_FACTOR:float = 0.915; // Trigger ratio
	var BD_QUALITY_DECAY:float = 0.6;     // range and contest decay
	var BD_QUALITY_TOLERANCE:float = 0.96;// Use the top x % of contest results
	var BD_QUALITY_REWARD:float = 10.0;    // Award weight
	var BD_QUALITY_STEP:float = 0.1;     // Award step (roaming speed)
	var BD_MINIMUM_CONTRIBUTIONS:float = 6;  //was 6 // At least x ranges must agree to process a result
	var BD_FINISH_LINE:float = 60.0;          // Contest values wil be normalized to this finish line
	// this is the 'funnel' that pulls ranges in / out of alignment based on trigger detection
	var BD_REWARD_TOLERANCES:float[] = [ 0.001, 0.005, 0.01, 0.02, 0.04, 0.08, 0.10, 0.15, 0.30 ];  // .1%, .5%, 1%, 2%, 4%, 8%, 10%, 15%
	var BD_REWARD_MULTIPLIERS:float[] =[ 20.0, 10.0, 8.0, 1.0, 1.0/2.0, 1.0/4.0, 1.0/8.0, 1/16.0, 1/32.0 ];

	var BPM_MIN:float;
	var BPM_MAX:float;

	var beat_counter:int;
	var half_counter:int;
	var quarter_counter:int;

	// current average (this sample) for range n
	var a_freq_range:float[];
	// moving average of frequency range n
	var ma_freq_range:float[];
	// moving average of moving average of frequency range n
	var maa_freq_range:float[];
	// timestamp of last detection for frequecy range n
	var last_detection:float[];

	// moving average of gap Lengths
	var ma_bpm_range:float[];
	// moving average of moving average of gap Lengths
	var maa_bpm_range:float[];

	// range n quality attribute, good match  = quality+, bad match  = quality-, min  = 0
	var detection_quality:float[];

	// current trigger state for range n
	var detection:System.Boolean[]; 


// Default configuration kernel
//BeatDetektor.config = BeatDetektor.config_default;


	var ma_quality_avg:int;
	var ma_quality_total:int;
	
	var bpm_contest:float[];
	var bpm_contest_lo:float[];
	
	var quality_total:float;
	var quality_avg:float;

	var current_bpm:float; 
	var current_bpm_lo:float; 

	var winning_bpm:float; 
	var win_val:float;
	var winning_bpm_lo:float; 
	var win_val_lo:float;


	var bpm_contest_highest;
	var bpm_contest_highest_lo;

	var win_bpm_int:int;
	var win_bpm_int_lo:int;

	var bpm_predict:float;

	var is_erratic:System.Boolean = false;
	var bpm_offset:float;
	var last_timer:float;
	var last_update:float;

	var bpm_timer:float;	

function Start()
{

	}

function Update () 
{

}

function init(bpm_minimum:float, bpm_maximum:float)
{
	if (typeof(bpm_minimum)=='undefined') bpm_minimum = 85.0;
	if (typeof(bpm_maximum)=='undefined') bpm_maximum = 169.0;	
	
	BPM_MIN = bpm_minimum;
	BPM_MAX = bpm_maximum;

	beat_counter = 0;
	half_counter = 0;
	quarter_counter = 0;

	// current average (this sample) for range n
	a_freq_range = new float[BD_DETECTION_RANGES];
	// moving average of frequency range n
	ma_freq_range = new float[BD_DETECTION_RANGES];
	// moving average of moving average of frequency range n
	maa_freq_range = new float[BD_DETECTION_RANGES];
	// timestamp of last detection for frequecy range n
	last_detection = new float[BD_DETECTION_RANGES];

	// moving average of gap Lengths
	ma_bpm_range = new float[BD_DETECTION_RANGES];
	// moving average of moving average of gap Lengths
	maa_bpm_range = new float[BD_DETECTION_RANGES];

	// range n quality attribute, good match  = quality+, bad match  = quality-, min  = 0
	detection_quality = new float[BD_DETECTION_RANGES];

	// current trigger state for range n
	detection = new System.Boolean[BD_DETECTION_RANGES];
	
	reset();
	
	
	Debug.Log("BeatDetektor("+BPM_MIN+","+BPM_MAX+") created.");
	
};

function reset()
{
//	var bpm_avg = 60.0/((this.BPM_MIN+this.BPM_MAX)/2.0);

	for (var i = 0; i < BD_DETECTION_RANGES; i++)
	{
		this.a_freq_range[i] = 0.0;
		this.ma_freq_range[i] = 0.0;
		this.maa_freq_range[i] = 0.0;
		this.last_detection[i] = 0.0;
		
		this.maa_bpm_range[i] = 60.0/this.BPM_MIN + ((60.0/this.BPM_MAX-60.0/this.BPM_MIN) * (i/BD_DETECTION_RANGES));	
		this.ma_bpm_range[i] = 	this.maa_bpm_range[i];
		
		this.detection_quality[i] = 0.0;
		this.detection[i] = false;
	}
	
	this.ma_quality_avg = 0;
	this.ma_quality_total = 0;
	
	this.bpm_contest = new float[2048];
	this.bpm_contest_lo = new float[2048];
	
	this.bpm_contest_highest = 0;
	this.bpm_contest_highest_lo = 0;
	
	this.quality_total = 0.0;
	this.quality_avg = 0.0;

	this.current_bpm = 0.0; 
	this.current_bpm_lo = 0.0; 

	this.winning_bpm = 0.0; 
	this.win_val = 0.0;
	this.winning_bpm_lo = 0.0; 
	this.win_val_lo = 0.0;

	this.win_bpm_int = 0;
	this.win_bpm_int_lo = 0;

	this.bpm_predict = 0;

	this.is_erratic = false;
	this.bpm_offset = 0.0;
	this.last_timer = 0.0;
	this.last_update = 0.0;

	this.bpm_timer = 0.0;
	this.beat_counter = 0;
	this.half_counter = 0;
	this.quarter_counter = 0;
};





function process(timer_seconds:float, fft_data:float[])
{
	if (!this.last_timer) { this.last_timer = timer_seconds; return; }	// ignore 0 start time
	if (this.last_timer > timer_seconds) { this.reset(); return; }
	
	var timestamp = timer_seconds;
	
	this.last_update = timer_seconds - this.last_timer;
	this.last_timer = timer_seconds;

	if (this.last_update > 1.0) { this.reset(); return; }

	var i:int;
	var x:int;
	var v:float;
	
	var bpm_Floor = 60.0/this.BPM_MAX;
	var bpm_Ceil = 60.0/this.BPM_MIN;
	
	var range_step = (fft_data.length / BD_DETECTION_RANGES);
	var range = 0;
	
		
	for (x=0; x<fft_data.length; x+=range_step)
	{
		this.a_freq_range[range] = 0;
		
		// accumulate frequency values for this range
		for (i = x; i<x+range_step; i++)
		{
			v = Mathf.Abs(fft_data[i]);
			this.a_freq_range[range] += v;
		}
		
		// average for range
		this.a_freq_range[range] /= range_step;
		
		// two sets of averages chase this one at a 
		
		// moving average, increment closer to a_freq_range at a rate of 1.0 / BD_DETECTION_RATE seconds
		this.ma_freq_range[range] -= (this.ma_freq_range[range]-this.a_freq_range[range])*this.last_update*BD_DETECTION_RATE;
		// moving average of moving average, increment closer to this.ma_freq_range at a rate of 1.0 / BD_DETECTION_RATE seconds
		this.maa_freq_range[range] -= (this.maa_freq_range[range]-this.ma_freq_range[range])*this.last_update*BD_DETECTION_RATE;
		
		// if closest moving average peaks above trailing (with a tolerance of BD_DETECTION_FACTOR) then trigger a detection for this range 
		var det = (this.ma_freq_range[range]*BD_DETECTION_FACTOR >= this.maa_freq_range[range]);
		
		// compute bpm clamps for comparison to gap lengths
		
		// clamp detection averages to input ranges
		if (this.ma_bpm_range[range] > bpm_Ceil) this.ma_bpm_range[range] = bpm_Ceil;
		if (this.ma_bpm_range[range] < bpm_Floor) this.ma_bpm_range[range] = bpm_Floor;
		if (this.maa_bpm_range[range] > bpm_Ceil) this.maa_bpm_range[range] = bpm_Ceil;
		if (this.maa_bpm_range[range] < bpm_Floor) this.maa_bpm_range[range] = bpm_Floor;
			
		var rewarded = false;
		
		// new detection since last, test it's quality
		if (!this.detection[range] && det)
		{
			// calculate length of gap (since start of last trigger)
			var trigger_gap = timestamp-this.last_detection[range];
			
			// trigger falls within acceptable range, 
			if (trigger_gap < bpm_Ceil && trigger_gap > (bpm_Floor))
			{		
				// compute gap and award quality
				
				// use our tolerances as a funnel to edge detection towards the most likely value
				for (i = 0; i < BD_REWARD_TOLERANCES.length; i++)
				{
					if (Mathf.Abs(this.ma_bpm_range[range]-trigger_gap) < this.ma_bpm_range[range]*BD_REWARD_TOLERANCES[i])
					{
						this.detection_quality[range] += BD_QUALITY_REWARD * BD_REWARD_MULTIPLIERS[i]; 
						rewarded = true;
					}
				}				
				
				if (rewarded) 
				{
					this.last_detection[range] = timestamp;
				}
			}
			else if (trigger_gap >= bpm_Ceil) // low quality, gap exceeds maximum time
			{
				// start a new gap test, next gap is guaranteed to be longer
				
				// test for 1/2 beat
				trigger_gap /= 2.0;

				if (trigger_gap < bpm_Ceil && trigger_gap > (bpm_Floor)) for (i = 0; i < BD_REWARD_TOLERANCES.length; i++)
				{
					if (Mathf.Abs(this.ma_bpm_range[range]-trigger_gap) < this.ma_bpm_range[range]*BD_REWARD_TOLERANCES[i])
					{
						this.detection_quality[range] += BD_QUALITY_REWARD * BD_REWARD_MULTIPLIERS[i]; 
						rewarded = true;
					}
				}
				
				
				// decrement quality if no 1/2 beat reward
				if (!rewarded) 
				{
					trigger_gap *= 2.0;
				}
				this.last_detection[range] = timestamp;	
			}
			
			if (rewarded)
			{
				var qmp = (this.detection_quality[range]/this.quality_avg)*BD_QUALITY_STEP;
				if (qmp > 1.0)
				{
					qmp = 1.0;
				}

				this.ma_bpm_range[range] -= (this.ma_bpm_range[range]-trigger_gap) * qmp;				
				this.maa_bpm_range[range] -= (this.maa_bpm_range[range]-this.ma_bpm_range[range]) * qmp;
			}
			else if (trigger_gap >= bpm_Floor && trigger_gap <= bpm_Ceil)
			{
				if (this.detection_quality[range] < this.quality_avg*BD_QUALITY_TOLERANCE && this.current_bpm)
				{
					this.ma_bpm_range[range] -= (this.ma_bpm_range[range]-trigger_gap) * BD_QUALITY_STEP;
					this.maa_bpm_range[range] -= (this.maa_bpm_range[range]-this.ma_bpm_range[range]) * BD_QUALITY_STEP;
				}
				this.detection_quality[range] -= BD_QUALITY_STEP;
			}
			else if (trigger_gap >= bpm_Ceil)
			{
				if ((this.detection_quality[range] < this.quality_avg*BD_QUALITY_TOLERANCE) && this.current_bpm)
				{
					this.ma_bpm_range[range] -= (this.ma_bpm_range[range]-this.current_bpm) * 0.5;
					this.maa_bpm_range[range] -= (this.maa_bpm_range[range]-this.ma_bpm_range[range]) * 0.5 ;
				}
				this.detection_quality[range]-= BD_QUALITY_STEP;
			}
			
		}
				
		if ((!rewarded && timestamp-this.last_detection[range] > bpm_Ceil) || (det && Mathf.Abs(this.ma_bpm_range[range]-this.current_bpm) > this.bpm_offset)) 
			this.detection_quality[range] -= this.detection_quality[range]*BD_QUALITY_STEP*BD_QUALITY_DECAY*this.last_update;
		
		// quality bottomed out, set to 0
		if (this.detection_quality[range] < 0.001) this.detection_quality[range]=0.001;
				
		this.detection[range] = det;		
		
		range++;
	}
		
	// total contribution weight
	this.quality_total = 0;
	
	// total of bpm values
	var bpm_total = 0;
	// number of bpm ranges that contributed to this test
	var bpm_contributions = 0;
	
	
	// accumulate quality weight total
	for (x=0; x<BD_DETECTION_RANGES; x++)
	{
		this.quality_total += this.detection_quality[x];
	}
	
	
	this.quality_avg = this.quality_total / BD_DETECTION_RANGES;
	
	
	if (this.quality_total)
	{
		// determine the average weight of each quality range
		this.ma_quality_avg += (this.quality_avg - this.ma_quality_avg) * this.last_update * BD_DETECTION_RATE/2.0;

		//Important rewrite here, not sure...
		
		//this.maa_quality_avg += (this.ma_quality_avg - this.maa_quality_avg) * this.last_update;
		//this.ma_quality_total += (this.quality_total - this.ma_quality_total) * this.last_update * BD_DETECTION_RATE/2.0;
		
		var maa_quality_avg:float;
		
		maa_quality_avg += (this.ma_quality_avg - maa_quality_avg) * this.last_update;
		this.ma_quality_total += (this.quality_total - this.ma_quality_total) * this.last_update * BD_DETECTION_RATE/2.0;

		this.ma_quality_avg -= 0.98*this.ma_quality_avg*this.last_update*3.0;
	}
	else
	{
		this.quality_avg = 0.001;
	}

	if (this.ma_quality_total <= 0) this.ma_quality_total = 0.001;
	if (this.ma_quality_avg <= 0) this.ma_quality_avg = 0.001;
	
	var avg_bpm_offset = 0.0;
	var offset_test_bpm = this.current_bpm;
	//var draft = new System.Collections.Generic.List.<float>();
	
	var draft:float[] = new float[2048];
	var highestDraft = 0;
	for (i = 0; i < 2048; i++)
	{
		draft[0] = 0;
	}
	
	if (this.quality_avg) for (x=0; x<BD_DETECTION_RANGES; x++)
	{
		// if this detection range weight*tolerance is higher than the average weight then add it's moving average contribution 
		if (this.detection_quality[x]*BD_QUALITY_TOLERANCE >= this.ma_quality_avg)
		{
			if (this.ma_bpm_range[x] < bpm_Ceil && this.ma_bpm_range[x] > bpm_Floor)
			{			
				bpm_total += this.maa_bpm_range[x];

				var draft_float = Mathf.Round((60.0/this.maa_bpm_range[x])*1000.0);
				
				draft_float = (Mathf.Abs(Mathf.Ceil(draft_float)-(60.0/this.current_bpm)*1000.0)<(Mathf.Abs(Mathf.Floor(draft_float)-(60.0/this.current_bpm)*1000.0)))?Mathf.Ceil(draft_float/10.0):Mathf.Floor(draft_float/10.0);
				var draft_int = parseInt(draft_float/10.0);
				//Debug.Log(draft_int);
			
				//if (draft_int) console.log(draft_int);
				
				//if (typeof(draft[draft_int]=='undefined'))
				//if (draft[draft_int] == null)
				//{				
				//draft[draft_int] = 0;				
				//}
				//draft[draft_int]+=this.detection_quality[x]/this.quality_avg;
				
				
				//Not sure if this is really doing the right thing? Does the add position actually relate 
				
				draft[draft_int] += this.detection_quality[x]/this.quality_avg;
				
				if (draft_int > highestDraft) highestDraft = draft_int;
				
				bpm_contributions++;
				if (offset_test_bpm == 0.0) offset_test_bpm = this.maa_bpm_range[x];
				else 
				{
					avg_bpm_offset += Mathf.Abs(offset_test_bpm-this.maa_bpm_range[x]);
				}
				
				
			}
		}
	}
		
	// if we have one or more contributions that pass criteria then attempt to display a guess
	var has_prediction = (bpm_contributions>=BD_MINIMUM_CONTRIBUTIONS)?true:false;

	var draft_winner:float=0;
	var win_val:int = 0;
	
	if (has_prediction) 
	{
		//for (var draft_i in draft)
		for(i =0; i < highestDraft; i++)
		{
			if (draft[i] > win_val)
			{
				win_val = draft[i];
				draft_winner = i;
			}
		}
		
		
		if (draft_winner)
		{
		this.bpm_predict = 60.0/(draft_winner/10.0);		
		}
		//Debug.Log(this.bpm_predict);
		
		
		avg_bpm_offset /= bpm_contributions;
		this.bpm_offset = avg_bpm_offset;
				
		if (!this.current_bpm)  
		{
			this.current_bpm = this.bpm_predict; 
		}
		
	}
	//Debug.Log(this.current_bpm + ":" + this.bpm_predict);
		
	if (this.current_bpm && this.bpm_predict) 
	{
	this.current_bpm -= (this.current_bpm-this.bpm_predict)*this.last_update;	
	}
	// hold a contest for bpm to find the current mode
	var contest_max=0;
	
	//for (var contest_i in this.bpm_contest)
	for(i =0; i < this.bpm_contest.Length; i++)//for(i =0; i < this.bpm_contest_highest; i++)	
	{
		if (contest_max < this.bpm_contest[i]) contest_max = this.bpm_contest[i]; 
		if (this.bpm_contest[i] > BD_FINISH_LINE/2.0)
		{
			var draft_int_lo = parseInt(Mathf.Round((i)/10.0));
			if (this.bpm_contest_lo[draft_int_lo] != this.bpm_contest_lo[draft_int_lo]) this.bpm_contest_lo[draft_int_lo] = 0;
			this.bpm_contest_lo[draft_int_lo]+= (this.bpm_contest[i]/6.0)*this.last_update;
		}
	}
		
	// normalize to a finish line
	if (contest_max > BD_FINISH_LINE) 
	{
		//for (var contest_i in this.bpm_contest)
		for(i =0; i < this.bpm_contest.Length; i++)
		{
			this.bpm_contest[i]=(this.bpm_contest[i]/contest_max)*BD_FINISH_LINE;
		}
	}

	contest_max = 0;
	//for (var contest_i in this.bpm_contest_lo)
	for(i =0; i < this.bpm_contest_lo.Length; i++)
	{
		if (contest_max < this.bpm_contest_lo[i]) contest_max = this.bpm_contest_lo[i]; 
	}

	// normalize to a finish line
	if (contest_max > BD_FINISH_LINE) 
	{
		//for (var contest_i in this.bpm_contest_lo)
		for(i =0; i < this.bpm_contest_lo.Length; i++)
		{
			this.bpm_contest_lo[i]=(this.bpm_contest_lo[i]/contest_max)*BD_FINISH_LINE;
		}
	}

	
	// decay contest values from last loop
	//for (contest_i in this.bpm_contest)
	for(i =0; i < this.bpm_contest.Length; i++)
	{
		this.bpm_contest[i]-=this.bpm_contest[i]*(this.last_update/BD_DETECTION_RATE);
	}
	
	// decay contest values from last loop
	//for (contest_i in this.bpm_contest_lo)
	for(i =0; i < this.bpm_contest_lo.Length; i++)
	{
		this.bpm_contest_lo[i]-=this.bpm_contest_lo[i]*(this.last_update/BD_DETECTION_RATE);
	}
	
	this.bpm_timer+=this.last_update;
	
	var winner = 0;
	var winner_lo = 0;
	
	
	//Debug.Log("bpm_timer:" + this.bpm_timer + "  winning_bpm:" + (this.winning_bpm/4.0) + "  this.current_bpm:" + this.current_bpm);
	
	// attempt to display the beat at the beat interval ;)
	if (this.bpm_timer > this.winning_bpm/4.0 && this.current_bpm)
	{		
		this.win_val = 0;
		this.win_val_lo = 0;

		if (this.winning_bpm) while (this.bpm_timer > this.winning_bpm/4.0) this.bpm_timer -= this.winning_bpm/4.0;
		
		// increment beat counter
		
		this.quarter_counter++;		
		this.half_counter= parseInt(this.quarter_counter/2);
		this.beat_counter = parseInt(this.quarter_counter/4);
		
		// award the winner of this iteration
		var idx = parseInt(Mathf.Round((60.0/this.current_bpm)*10.0));
		//Debug.Log(idx);
		//if (typeof(this.bpm_contest[idx])=='undefined') 
		
		if (this.bpm_contest[idx] > 0)
		{
		this.bpm_contest[idx] = 0;
		}
		
		this.bpm_contest[idx]+=BD_QUALITY_REWARD;
		
		
		// find the overall winner so far
		//for (var contest_i in this.bpm_contest)
		for(i =0; i < this.bpm_contest.Length; i++)
		{
			if (this.win_val < this.bpm_contest[i])
			{
				winner = i;
				this.win_val = this.bpm_contest[i];
			}
		}
		
		if (winner > 0) //changed here assumed meant this
		{
			this.win_bpm_int = parseInt(winner);
			this.winning_bpm = (60.0/(winner/10.0));
		}
		
		// find the overall winner so far
		//for (var contest_i in this.bpm_contest_lo)
		for(i =0; i < this.bpm_contest_lo.Length; i++)
		{
			if (this.win_val_lo < this.bpm_contest_lo[i])
			{
				winner_lo = i;
				this.win_val_lo = this.bpm_contest_lo[i];
			}
		}
		
		if (winner_lo > 0) //changed here assumed meant this
		{
			this.win_bpm_int_lo = parseInt(winner_lo);
			this.winning_bpm_lo = 60.0/winner_lo;
		}
		
		
	// 	if ((this.beat_counter % 4) == 0) Debug.Log("BeatDetektor("+this.BPM_MIN+","+this.BPM_MAX+"): [ Current Estimate: "+winner+" BPM ] [ Time: "+(parseInt(timer_seconds*1000.0)/1000.0)+"s, Quality: "+(parseInt(this.quality_total*1000.0)/1000.0)+", Rank: "+(parseInt(this.win_val*1000.0)/1000.0)+", Jitter: "+(parseInt(this.bpm_offset*1000000.0)/1000000.0)+" ]");
	}

};

/* needs to split out into seperate classes I think?

BeatDetektor.modules = new Object(); 
BeatDetektor.modules.vis = new Object();

// simple bass kick visualizer assistant module
BeatDetektor.modules.vis.BassKick = function()
{
	this.is_kick = false;
};

BeatDetektor.modules.vis.BassKick.prototype.process = function(det)
{
	this.is_kick = ((det.detection[0] && det.detection[1]) || (det.ma_freq_range[0]/det.maa_freq_range[0])>1.4);
};

BeatDetektor.modules.vis.BassKick.prototype.isKick = function()
{
	return this.is_kick;
};


// simple vu spectrum visualizer assistant module
BeatDetektor.modules.vis.VU = function()
{
	this.vu_levels = new Array();	
};

BeatDetektor.modules.vis.VU.prototype.process = function(detektor)
{
	var det = detektor;
	
	var det_max = 0.0;

	if (!this.vu_levels.Length)
	{
		for (var i = 0; i < det.config.BD_DETECTION_RANGES; i++)
		{
			this.vu_levels[i] = 0;
		}
	}

	for (var i = 0; i < det.config.BD_DETECTION_RANGES; i++)
	{
		var det_val = (det.a_freq_range[i]/det.maa_freq_range[i]);	
		if (det_val > det_max) det_max = det_val;
	}		

	if (det_max == 0) det_max = 1.0;

	for (var i = 0; i < det.config.BD_DETECTION_RANGES; i++)
	{
		var det_val = (det.ma_freq_range[i]/det.maa_freq_range[i]);

		if (det_val > 1.0)
		{
			det_val -= 1.0;
			
			if (det_val > this.vu_levels[i]) 
				this.vu_levels[i] = det_val;
			else if (det.current_bpm) this.vu_levels[i] -= (this.vu_levels[i]-det_val)*det.last_update*(1.0/det.current_bpm)*3.0;
		}
		else
		{
			if (det.current_bpm) this.vu_levels[i] -= (det.last_update/det.current_bpm)*2.0;
		}

		if (this.vu_levels[i] < 0) this.vu_levels[i] = 0;
	}
};


// returns vu level for BD_DETECTION_RANGES range[x]
BeatDetektor.modules.vis.VU.prototype.getLevel = function(x)
{
	return this.vu_levels[x];
};

*/
}
