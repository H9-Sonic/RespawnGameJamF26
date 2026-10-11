using Godot;
using System;

public partial class GameMaster : Node2D
{

	[ExportGroup("References")]
	[Export]
	PackedScene skeletonScene;
	[Export]
	Player player;
	[Export]
	Node2D spawnPointsParent;
	[Export]
	ShopMenu shop;

	[ExportGroup("Waves")]
	[Export]
	int baseEnemies = 3;
	[Export]
	int EnemiesPerWave = 2;
	[Export]
	float SpawnInt = 0.6f;
	[Export]
	float firstWaveDelay = 3f;

	[ExportGroup("Coins")]
	[Export(PropertyHint.Range, "0,1,0.05")]
	float CoinDropChance = 0.4f;
	[Export]
	int wavesPerCoinBonus = 3;
	[Export]
	int minCoins = 1;
	[Export]
	int maxCoins = 3;

	[ExportGroup("Shop")]
	[Export]
	float ShopTimer = 30f;

	[ExportGroup("HUD")]
	[Export]
	Label waveLabel;
	[Export]
	Label coinsLabel;

	[ExportGroup("Scaling")]
	[Export]
	float healthScalePerWave = 0.15f;
	[Export]
	float damageScalePerWave = 0.10f;

	enum Phase {Starting, Spawning, Fighting, Shop, Over }

	Phase _phase = Phase.Starting;
	int _wave;
	int _toSpawn;
	int _alive;
	int _coins;
	int _coinsThisWave;
	double _timer;
	readonly RandomNumberGenerator _rng = new(); 
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Shop pauses the tree, so gameMaster needs to run count to continue
		ProcessMode = ProcessModeEnum.Always;

		_rng.Randomize();
		shop.CoinsSpent += amount => 
		{
			_coins -= amount;
			UpdateHUD();
		};
		UpdateHUD();
		_timer = firstWaveDelay;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(_phase != Phase.Over && player.IsDead)
		{
			_phase = Phase.Over;
			shop.Close();
			GD.Print($"Game Over! You reached wave {_wave} with {_coins} coins");
			return;
		}

		switch (_phase)
		{
			case Phase.Starting:
				_timer -= delta;
				if(_timer <= 0)
					StartWave();
				break;

			case Phase.Spawning:
			    _timer -= delta;
				if(_timer <= 0)
				{
					SpawnSkeleton();
					_toSpawn--;
					_timer = SpawnInt;
					if(_toSpawn <= 0)
						_phase = Phase.Fighting;
				}
				break;
			case Phase.Fighting:
				if (_alive <= 0)
					EndWave();
				break;
			case Phase.Shop:
				_timer -= delta;
				shop.SetTimer(_timer);
				if(!shop.Visible || _timer <= 0)
				{
					shop.Close();
					StartWave();
				}
				break;
		}
	}

	void StartWave()
	{
		_wave++;
		_coinsThisWave = 0;
		_toSpawn = baseEnemies + (_wave - 1) * EnemiesPerWave;
		_timer = 0;
		_phase = Phase.Spawning;
		GD.Print($"Wave {_wave}: {_toSpawn} enemies approaching!");
		UpdateHUD();
	}

	void EndWave()
	{
		_phase = Phase.Shop;
		_timer = ShopTimer;
		shop.Open(_wave, _coinsThisWave, _coins);
		shop.SetTimer(_timer);

	}

	void SpawnSkeleton()
	{
		var skelly = skeletonScene.Instantiate<Skeleton>();
		player.GetParent().AddChild(skelly);

		float healthMult = 1.0f + (_wave - 1) * healthScalePerWave;
		float damageMult = 1.0f + (_wave - 1) * damageScalePerWave;
		skelly.WaveScale(healthMult, damageMult);

		var points = spawnPointsParent.GetChildren();
		var point = (Node2D)points[_rng.RandiRange(0, points.Count - 1)];
		skelly.GlobalPosition = point.GlobalPosition;

		skelly.Died += OnSkeletonDied;
		_alive++;
	}

	void OnSkeletonDied(Skeleton skel)
	{
		_alive--;

		if(_rng.Randf() < CoinDropChance)
		{
			int bonus = _wave / wavesPerCoinBonus;
			int amount = _rng.RandiRange(minCoins, maxCoins + bonus);
			_coins += amount;
			_coinsThisWave += amount;
			UpdateHUD();
		}
	}

	void UpdateHUD()
	{
		if(IsInstanceValid(waveLabel))
			waveLabel.Text = $"Wave {_wave}";
		if(IsInstanceValid(coinsLabel))
			coinsLabel.Text = $"Coins {_coins}";
	}
}
