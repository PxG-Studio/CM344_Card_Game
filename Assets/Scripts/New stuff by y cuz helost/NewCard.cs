using UnityEngine;
using NewCardData;  // Import the namespace

namespace CardGame.Core
{
    /// <summary>
    /// Runtime representation of a NewCardData instance
    /// </summary>
    public class NewCard
    {
        // Use fully qualified name: Namespace.ClassName
        public NewCardData.NewCardData Data { get; private set; }
        public int InstanceID { get; private set; }
        
        // Runtime modifiable directional stats
        public int CurrentTopStat { get; set; }
        public int CurrentRightStat { get; set; }
        public int CurrentDownStat { get; set; }
        public int CurrentLeftStat { get; set; }
        
        public bool IsPlayable { get; set; }
        public bool IsExhausted { get; set; }
        
        private static int _nextInstanceID = 0;
        
        // Use fully qualified name in constructor parameter
        public NewCard(NewCardData.NewCardData data)
        {
            Data = data;
            InstanceID = _nextInstanceID++;
            
            // Initialize runtime stats from data
            CurrentTopStat = data.TopStat;
            CurrentRightStat = data.RightStat;
            CurrentDownStat = data.DownStat;
            CurrentLeftStat = data.LeftStat;
            
            IsPlayable = true;
            IsExhausted = false;
        }
        
        public void ResetToBaseStats()
        {
            CurrentTopStat = Data.TopStat;
            CurrentRightStat = Data.RightStat;
            CurrentDownStat = Data.DownStat;
            CurrentLeftStat = Data.LeftStat;
            IsExhausted = false;
        }
        
        public void ModifyStats(int topDelta = 0, int rightDelta = 0, int downDelta = 0, int leftDelta = 0)
        {
            CurrentTopStat = Mathf.Max(0, CurrentTopStat + topDelta);
            CurrentRightStat = Mathf.Max(0, CurrentRightStat + rightDelta);
            CurrentDownStat = Mathf.Max(0, CurrentDownStat + downDelta);
            CurrentLeftStat = Mathf.Max(0, CurrentLeftStat + leftDelta);
        }
    }
}