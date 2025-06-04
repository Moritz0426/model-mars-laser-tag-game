using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Shared;
using Mars.Common.Core.Random;
using Mars.Interfaces.Environments;
using ServiceStack;

namespace LaserTagBox.Model.Mind;

public sealed class TeamCommuniation
{
    
    private static readonly Lazy<TeamCommuniation> _instance =
        new Lazy<TeamCommuniation>(() => new TeamCommuniation());
    public static TeamCommuniation Instance => _instance.Value;

    public HashSet<Position> _waterPositions;
    public HashSet<Position> _barrierPositions;
    public HashSet<Position> _hillPositions;
    public HashSet<Position> _ditchPositions;
    
    
    private TeamCommuniation()
    {
        _waterPositions = new HashSet<Position>();
        _barrierPositions = new HashSet<Position>();
        _hillPositions = new HashSet<Position>();
        _ditchPositions = new HashSet<Position>();
        
    }

    public HashSet<Position> _WaterPositions
    {
        get => _waterPositions;
        set => _waterPositions = value;
    }

    public HashSet<Position> _BarrierPositions
    {
        get => _barrierPositions;
        set => _barrierPositions = value;
    }

    public HashSet<Position> _HillPositions
    {
        get => _hillPositions;
        set => _hillPositions = value;
    }

    public HashSet<Position> _DitchPositions
    {
        get => _ditchPositions;
        set => _ditchPositions = value;
    }
    
}