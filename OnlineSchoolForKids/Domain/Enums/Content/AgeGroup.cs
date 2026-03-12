using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Content;

public enum AgeGroup
{
    ForParents = 0,
    ForEducators = 1,

    Toddlers = 2,   // 1–3 years
    Preschool = 3,   // 3–5 years
    EarlyPrimary = 4,   // 5–8 years
    LatePrimary = 5,   // 8–12 years
    Tweens = 6,   // 10–13 years
    Teenagers = 7,   // 13–18 years
}