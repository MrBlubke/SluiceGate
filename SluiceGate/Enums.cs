﻿namespace SluiceGate
{
    public enum Length
    {
        Small = 1,
        Medium = 2,
        Long = 3,
    };

    public enum StateOfSluice
    {
        Up = 1,
        EnRoute = 2,
        Down = 3
    };

    public enum CanBeAdded
    {
        Yes = 0,
        NoNotCurrently = 1
    }
}