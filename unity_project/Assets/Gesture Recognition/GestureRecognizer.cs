using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DTW;
using UnityEngine;

public class Gesture {
	public Gesture(Vector2[] moves) {
		this.Moves = moves;
		this.Normalize();
	}

	public Vector2[] Moves { get; private set; }

	private void Normalize() {
		for (int i = 0; i < this.Moves.Length; ++i) {
			this.Moves[i] = this.Moves[i].normalized;
		}
	}

	public float DistanceToGesture(Gesture other) {
		// We calculate distance separately for x and y and then combine them
		float xdistance = SimpleDTW.get(this.Moves.Select(e => e.x).ToArray(), other.Moves.Select(e => e.x).ToArray());
		float ymatch = SimpleDTW.get(this.Moves.Select(e => e.y).ToArray(), other.Moves.Select(e => e.y).ToArray());
		return Mathf.Sqrt(xdistance*xdistance + ymatch*ymatch);
	}
}

public class NamedGesture : Gesture {
	public readonly string Name;

	public NamedGesture(Vector2[] moves, string name) : base(moves) {
		this.Name = name;
	}

	public override string ToString() {
		return string.Format("NamedGesture({0})", this.Name);
	}
}

public struct GestureMatch {
	public float Distance;
	public NamedGesture Gesture;
}

public class GestureRecognizer {
	private static GestureRecognizer sharedInstance;
	private readonly List<NamedGesture> data = new List<NamedGesture>();

	public GestureRecognizer() {
		this.InitializeGesturesDatabase();
	}

	private void InitializeGesturesDatabase() {
		this.AddGesture(new[] {new Vector2(-1, 0)}, "hline");
		this.AddGesture(new[] {new Vector2(1, 0)}, "hline");

		this.AddGesture(new[] {new Vector2(0, -1)}, "vline");
		this.AddGesture(new[] {new Vector2(0, 1)}, "vline");

		this.AddGesture(new[] {new Vector2(1, 0), new Vector2(-1, -1), new Vector2(1, 0)}, "zet");

		for (float x = 0.5f; x <= 2.0f; x += 0.25f) {
			for (float y = 0.5f; y <= 2.0f; y += 0.25f) {
				this.AddGesture(new[] {new Vector2(x, y), new Vector2(x, -y)}, "vup");
				this.AddGesture(new[] {new Vector2(x, -y), new Vector2(x, y)}, "vdown");
			}
		}
	}

	public void AddGesture(Vector2[] moves, string name) {
		this.AddGesture(new NamedGesture(moves, name));
	}

	public void AddGesture(NamedGesture gesture) {
		this.data.Add(gesture);
	}

	public NamedGesture RecognizeGesture(Gesture gesture) {
		var matches = this.GetSortedMatchesForGesture(gesture);

		// if two gestures have Distance greater than threshold, we say "no match"
		float threshold = 3f;

		if (matches.Count() == 0 || matches.First().Distance > threshold) {
			throw new GestureNotFoundException();
		}

		GestureMatch fm = matches.First();
		Debug.Log(string.Format("Successfully matched gesture: {0} (Distance: {1})", fm.Gesture.Name, fm.Distance));
		return fm.Gesture;
	}

	public IOrderedEnumerable<GestureMatch> GetSortedMatchesForGesture(Gesture gesture) {
		IOrderedEnumerable<GestureMatch> matches = this.data
			.Select(g => new GestureMatch {Gesture = g, Distance = g.DistanceToGesture(gesture)})
			.OrderBy(match => match.Distance);
		return matches;
	}

	public static GestureRecognizer GetSharedInstance() {
		if (sharedInstance == null) {
			sharedInstance = new GestureRecognizer();
		}

		return sharedInstance;
	}
}

public class MouseGestures {
	public Gesture getGestureFromPoints(ArrayList points) {
		var list = new List<Vector2>();

		for (int i = 1; i < points.Count; ++i) {
			var start = (Vector3) points[i - 1];
			var end = (Vector3) points[i];

			Vector3 distance = end - start;

			list.Add(new Vector2(distance.x, distance.y));
		}

		list = this.filterAccelerations(list);
		return new Gesture(list.ToArray());
	}

	private List<Vector2> filterAccelerations(List<Vector2> accelerations) {
		return this.filterAccelerationsByDirection(
			this.filterAccelerationsByMagnitude(accelerations));
	}

	private List<Vector2> filterAccelerationsByMagnitude(List<Vector2> accelerations) {
		float idleAccelerationThreshold = 5f;
		return accelerations.Where(v => Vector3.Magnitude(v) > idleAccelerationThreshold).ToList();
	}

	private List<Vector2> filterAccelerationsByDirection(List<Vector2> accelerations) {
		float angleThresholdDegrees = 20.0f;

		List<Vector2> list;
		if (accelerations.Count < 2) {
			list = accelerations;
		}
		else {
			list = new List<Vector2>();
			list.Add(accelerations[0]);

			for (int i = 1; i < accelerations.Count; ++i) {
				Vector2 a = list[list.Count - 1];
				Vector2 b = accelerations[i];

				float angle = Vector2.Angle(a, b);
				if (Mathf.Abs(angle) > angleThresholdDegrees) {
					list.Add(b);
				}
			}
		}

		return list;
	}
}