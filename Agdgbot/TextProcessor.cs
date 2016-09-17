using System.Collections.Generic;

namespace Agdgbot
{
    public class TextProcessor
    {
        public TextProbability probability;
        public string label;

        public class TextProbability
        {
            public float neg;
            public float neutral;
            public float pos;
        }
    }


}