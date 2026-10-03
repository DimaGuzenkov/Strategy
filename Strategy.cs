namespace Stratagy;

public sealed class Strategy : ISkipStrategy
{
    private const int SubjectCount = 6;

    private const double SkipThreshold = 0.003;

    public string Name => "Guzenkov Dmitrii 23207";

    private readonly List<SubjectHypothesis> _hypotheses = new(SubjectCount);
    private int _lastUpdatedDay;
    private bool[] _yesterdayQ = new bool[SubjectCount];

    private void Init()
    {
        _hypotheses.Clear();
        for (int i = 0; i < SubjectCount; ++i)
        {
            _hypotheses.Add(new SubjectHypothesis());
        }
    }

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        if (day == 1) Init();

        while (_lastUpdatedDay < day - 1)
        {
            int d = _lastUpdatedDay + 1;
            UpdateFromHistory(d, history);
            _lastUpdatedDay = d;
        }

        var decisions = new bool[SubjectCount];
        for (int i = 0; i < SubjectCount; i++)
        {
            double p = _hypotheses[i].Visiting(_yesterdayQ);
            decisions[i] = p >= SkipThreshold;
        }
        return decisions;
    }

    private void UpdateFromHistory(int day, IReadOnlyStudentHistory history)
    {
        var todayQ = new bool[SubjectCount];
        for (int i = 0; i < SubjectCount; i++)
        {
            var asked = history.WasAsked(day, (Subject)i);
            todayQ[i] = asked ?? false;
        }

        for (int i = 0; i < SubjectCount; i++)
            _hypotheses[i].UpdateHypothesis(_yesterdayQ, todayQ[i]);

        _yesterdayQ = todayQ;
    }

    private interface IHypothesis
    {
        double W { get; }
        void UpdateW(bool[] prevQ, bool obs);
        double Predict(bool[] prevQ);
        void Normalize(double n);
    }

    private class RandomHypothes : IHypothesis
    {
        public double W { get; private set; } = 1.0 / 3.0;
        public void UpdateW(bool[] prevQ, bool obs) => W /= 2;
        public double Predict(bool[] prevQ) => W / 2;
        public void Normalize(double n) => W /= n;
    }

    private class AHypothes(int A) : IHypothesis
    {
        public double W { get; private set; } = 1.0 / 18.0;

        public void UpdateW(bool[] prevQ, bool obs)
        {
            if (prevQ[A] ^ obs) W = 0;
        }
        public double Predict(bool[] prevQ) => prevQ[A] ? W : 0.0;
        public void Normalize(double n) => W /= n;
    }

    private record class ABHypothes(int A, int B) : IHypothesis
    {
        public double W { get; private set; } = 1.0 / 45.0;

        public void UpdateW(bool[] prevQ, bool obs)
        {
            if (prevQ[A] ^ prevQ[B] ^ obs) W = 0;
        }
        public double Predict(bool[] prevQ) => prevQ[A] ^ prevQ[B] ? W : 0.0;
        public void Normalize(double n) => W /= n;
    }


    private class SubjectHypothesis
    {
        private readonly List<IHypothesis> hypothses = new(22);

        public SubjectHypothesis()
        {
            hypothses.Add(new RandomHypothes());
            for (int a = 0; a < SubjectCount; a++)
                hypothses.Add(new AHypothes(a));
            for (int a = 0; a < SubjectCount; a++)
            {
                for (int b = a + 1; b < SubjectCount; b++)
                    hypothses.Add(new ABHypothes(a, b));
            }
        }

        public double Visiting(bool[] yestarday)
        {
            return hypothses.Sum(h => h.Predict(yestarday));
        }

        public void UpdateHypothesis(bool[] yestarday, bool observed)
        {
            hypothses.ForEach(h => h.UpdateW(yestarday, observed));
            hypothses.RemoveAll(h => h.W == 0);
            double total = hypothses.Sum(h => h.W);
            hypothses.ForEach(h => h.Normalize(total));
        }
    }
}