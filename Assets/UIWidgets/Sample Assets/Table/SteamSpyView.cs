﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UIWidgets;

namespace UIWidgetsSamples {
	public enum SteamSpySortFields
	{
		Name,
		ScoreRank,
		Owners,
		Players,
		PlayersIn2Week,
		Time,
	}

	public class SteamSpyView : ListViewCustom<SteamSpyComponent,SteamSpyItem> {
		bool isSteamSpyViewStarted;

		public override void Start()
		{
			if (isSteamSpyViewStarted)
			{
				return ;
			}

			isSteamSpyViewStarted = true;

			sortComparers = new Dictionary<SteamSpySortFields,Comparison<SteamSpyItem>>(){
				{SteamSpySortFields.Name, NameComparer},
				{SteamSpySortFields.ScoreRank, ScoreRankComparer},
				{SteamSpySortFields.Owners, OwnersComparer},
				{SteamSpySortFields.Players, PlayersComparer},
				{SteamSpySortFields.PlayersIn2Week, PlayersIn2WeekComparer},
				{SteamSpySortFields.Time, TimeComparer},
			};

			Sort = false;

			base.Start();

			StartCoroutine(LoadData());
		}

		void OnEnable()
		{
			if (isSteamSpyViewStarted && DataSource.Count==0)
			{
				StartCoroutine(LoadData());
			}
		}

		IEnumerator LoadData()
		{
			WWW www = new WWW("https://ilih.ru/steamspy/");
			yield return www;

			var lines = www.text.Split('\n');

			DataSource.BeginUpdate();

			DataSource.Clear();

			lines.ForEach(ParseLine);

			DataSource.EndUpdate();
		}

		void ParseLine(string line)
		{
			if (line=="")
			{
				return ;
			}
			var info = line.Split('\t');

			var item = new SteamSpyItem(){
				Name = info[0],
				ScoreRank = (info[1]=="") ? -1 : int.Parse(info[1]),

				Owners = int.Parse(info[2]),
				OwnersVariance = int.Parse(info[3]),

				Players = int.Parse(info[4]),
				PlayersVariance = int.Parse(info[5]),

				PlayersIn2Week = int.Parse(info[6]),
				PlayersIn2WeekVariance = int.Parse(info[7]),

				AverageTimeIn2Weeks = int.Parse(info[8]),
				MedianTimeIn2Weeks = int.Parse(info[9]),
			};
			DataSource.Add(item);
		}

		protected override void SetData(SteamSpyComponent component, SteamSpyItem item)
		{
			component.SetData(item);
		}
		
		protected override void HighlightColoring(SteamSpyComponent component)
		{
			//base.HighlightColoring(component);

			component.Name.color = HighlightedColor;
			component.ScoreRank.color = HighlightedColor;
			component.Owners.color = HighlightedColor;
			component.Players.color = HighlightedColor;
			component.PlayersIn2Week.color = HighlightedColor;
			component.TimeIn2Week.color = HighlightedColor;
		}
		
		protected override void SelectColoring(SteamSpyComponent component)
		{
			//base.SelectColoring(component);

			component.Name.color = SelectedColor;
			component.ScoreRank.color = SelectedColor;
			component.Owners.color = SelectedColor;
			component.Players.color = SelectedColor;
			component.PlayersIn2Week.color = SelectedColor;
			component.TimeIn2Week.color = SelectedColor;
		}
		
		protected override void DefaultColoring(SteamSpyComponent component)
		{
			//base.DefaultColoring(component);

			component.Name.color = DefaultColor;
			component.ScoreRank.color = DefaultColor;
			component.Owners.color = DefaultColor;
			component.Players.color = DefaultColor;
			component.PlayersIn2Week.color = DefaultColor;
			component.TimeIn2Week.color = DefaultColor;
		}

		SteamSpySortFields currentSortField = SteamSpySortFields.Players;

		Dictionary<SteamSpySortFields,Comparison<SteamSpyItem>> sortComparers;

		public void ToggleSort(SteamSpySortFields field)
		{
			if (field==currentSortField)
			{
				DataSource.Reverse();
			}
			else if (sortComparers.ContainsKey(field))
			{
				currentSortField = field;

				DataSource.Sort(sortComparers[field]);
			}
		}

		public void SortByName()
		{
			ToggleSort(SteamSpySortFields.Name);
		}

		public void SortByScoreRank()
		{
			ToggleSort(SteamSpySortFields.ScoreRank);
		}

		public void SortByOwners()
		{
			ToggleSort(SteamSpySortFields.Owners);
		}

		public void SortByPlayers()
		{
			ToggleSort(SteamSpySortFields.Players);
		}

		public void SortByPlayersIn2Week()
		{
			ToggleSort(SteamSpySortFields.PlayersIn2Week);
		}

		public void SortByTime()
		{
			ToggleSort(SteamSpySortFields.Time);
		}

		static protected int NameComparer(SteamSpyItem x, SteamSpyItem y)
		{
			return x.Name.CompareTo(y.Name);
		}

		static protected int ScoreRankComparer(SteamSpyItem x, SteamSpyItem y)
		{
			return x.ScoreRank.CompareTo(y.ScoreRank);
		}

		static protected int OwnersComparer(SteamSpyItem x, SteamSpyItem y)
		{
			return x.Owners.CompareTo(y.Owners);
		}

		static protected int PlayersComparer(SteamSpyItem x, SteamSpyItem y)
		{
			return x.Players.CompareTo(y.Players);
		}

		static protected int PlayersIn2WeekComparer(SteamSpyItem x, SteamSpyItem y)
		{
			return x.PlayersIn2Week.CompareTo(y.PlayersIn2Week);
		}

		static protected int TimeComparer(SteamSpyItem x, SteamSpyItem y)
		{
			return x.AverageTimeIn2Weeks.CompareTo(y.AverageTimeIn2Weeks);
		}
	}
}