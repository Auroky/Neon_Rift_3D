using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// OUR local score storage; gameplay still belongs to Game3D and the actors.
// Save every completed round, and display only the best five.
public sealed class RoundScore
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public int Score { get; set; }
	public int Kills { get; set; }
	public double SecondsPlayed { get; set; }
	public DateTime PlayedAt { get; set; }
	public string Result { get; set; } = "";
}

public sealed class ScoreHistory
{
	private readonly string _path = ProjectSettings.GlobalizePath("user://score_history.json");
	private readonly List<RoundScore> _rounds = new();
	private bool _loadFailed;

	public ScoreHistory()
	{
		try
		{
			if (File.Exists(_path))
			{
				var saved = JsonSerializer.Deserialize<List<RoundScore>>(File.ReadAllText(_path));
				if (saved == null || saved.Any(round => round == null || round.Score < 0
					|| round.Kills < 0 || !double.IsFinite(round.SecondsPlayed)
					|| round.SecondsPlayed < 0 || round.SecondsPlayed > 60))
				{
					throw new JsonException("Invalid score history.");
				}
				_rounds.AddRange(saved);
			}
		}
		catch (Exception error) when (error is IOException || error is UnauthorizedAccessException
			|| error is JsonException)
		{
			// Keep an unreadable existing save intact rather than overwrite old records.
			_loadFailed = true;
			GD.PushWarning("Could not read score history: " + error.Message);
		}
	}

	public bool Record(RoundScore round)
	{
		if (_rounds.Any(previous => previous.Id == round.Id)) return true;
		_rounds.Add(round);
		if (_loadFailed) return false;
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
			string temporary = _path + ".tmp";
			File.WriteAllText(temporary, JsonSerializer.Serialize(_rounds,
				new JsonSerializerOptions { WriteIndented = true }));
			File.Move(temporary, _path, true);
			return true;
		}
		catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
		{
			GD.PushWarning("Could not save this round: " + error.Message);
			return false;
		}
	}

	private IEnumerable<RoundScore> Ranked() => _rounds
		.OrderByDescending(round => round.Score)
		.ThenByDescending(round => round.PlayedAt)
		.ThenBy(round => round.Id, StringComparer.Ordinal);

	public RoundScore[] TopFive() => Ranked().Take(5).ToArray();

	public int RankOf(string id) => Ranked().ToList().FindIndex(round => round.Id == id) + 1;
}
