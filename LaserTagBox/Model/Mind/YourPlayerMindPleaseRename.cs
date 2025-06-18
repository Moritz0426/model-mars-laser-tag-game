using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LaserTagBox.Model.Items;
using LaserTagBox.Model.Shared;
using Mars.Common.Core.Random;
using Mars.Interfaces.Environments;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ServiceStack;

namespace LaserTagBox.Model.Mind;

public class RuledPlayerMind : AbstractPlayerMind
{
    private PlayerMindLayer _mindLayer;
    private Position _goal;
    private bool _isFleeing;

    public override void Init(PlayerMindLayer mindLayer)
    {
        _mindLayer = mindLayer;
    }

    public override void Tick()
    {
        _isFleeing = false;
        maybeReload();
        List<EnemySnapshot> enemys = maybeShoot(); //maybe he is fleeing from now on
        if (!_isFleeing)
        {
            decideMove(enemys);
        }
        useUnusedPoints();





        /*
         if (Body.ActionPoints < 10)
        {
            return;  //TODO execution order fix
        }
        var enemies = Body.ExploreEnemies1();
        if (enemies.Any())
        {
            _goal = enemies.First().Position.Copy();
            if (Body.RemainingShots == 0) Body.Reload3();
            Body.Tag5(enemies.First().Position);
        }

        if (_goal == null || Body.GetDistance(_goal) == 1)
        {
            var newX = RandomHelper.Random.Next(_mindLayer.Width);
            var newY = RandomHelper.Random.Next(_mindLayer.Height);
            _goal = Position.CreatePosition(newX, newY);
        }

        var moved = Body.GoTo(_goal);
        if (!moved) _goal = null;
        */

    }

    /**
     * Lädt nach wenn keine Schüsse mehr im Magazin.
     */
    private void maybeReload()
    {
        if(Body.RemainingShots == 0) Body.Reload3();
    }

    private void flee()
    {
        List<Position> ditchesPositions = Body.ExploreDitches1();
        Position closestditch;
        if(ditchesPositions.Count != 0)
        {
            closestditch = ditchesPositions.First();
            foreach (var pos in ditchesPositions)
            {
                if (Body.GetDistance(pos) < Body.GetDistance(closestditch))
                    closestditch = pos;
            }
            Body.GoTo(closestditch);
        }
        else
        {
            if (Body.ActionPoints >= 2)
            {
                List<FlagSnapshot> flags = Body.ExploreFlags2();
                foreach (var flag in flags)
                {
                    if (flag.Team.Equals(Body.Color))
                    {
                        Body.GoTo(flag.Position); 
                    }
                }
            }
            
        }
        Body.ChangeStance2(Stance.Lying);
    }

    private void shoot(Position target)
    {
        if(Body.ActionPoints >= 7)
            Body.ChangeStance2(Stance.Lying);
        Body.Tag5(target);
    }
    /*
     * 
     */
    private List<EnemySnapshot> maybeShoot() //TODO: anstatt als Parameter zurück zu geben, in Map Speichern
    {
        List<EnemySnapshot> enemyList = Body.ExploreEnemies1();
        if (!enemyList.IsEmpty())
        {
            if (enemyList.Count() >= 2)
            {
                flee();
                _isFleeing = true;
            }
            
            if (Body.ActionPoints >= 8)
            {
                //Nahesten Enemy erhalten
                Position closestEnemyPosition = getClosestEnemy(enemyList);
                
                //Erhalte näheste Barrel zu enemy. Null, wenn keine Barrel in Sichtweite
                List<Position> explosiveBarrelsList = Body.ExploreExplosiveBarrels1();
                Position closestBarrelToEnemy = getClosestBarrelToEnemy(explosiveBarrelsList, closestEnemyPosition);

                //wenn Gegner im Radius von 3 von Barrel ist, auf Barrel schießen
                if (closestBarrelToEnemy != null && getDistance(closestEnemyPosition, closestBarrelToEnemy) < 4.0)
                {
                    shoot(closestBarrelToEnemy);
                }
                else //einfach so auf gegner schießen. 
                {
                    shoot(closestEnemyPosition); //ist nicht null, weil oben geprüft
                }
                
            }
            else
            {
                //Nahesten Enemy erhalten
                Position closestEnemyPosition = getClosestEnemy(enemyList);
                shoot(closestEnemyPosition);
            }
        }

        return enemyList;
    }

    /// Determines and retrieves the position of the closest enemy from a given list of enemies.
    /// <param name="enemies">A list of enemies to evaluate for proximity.</param> <return>The position of the closest enemy, or null if the list is empty or null.</return>
    /// /
    private Position getClosestEnemy(List<EnemySnapshot> enemys)
    {
        if (enemys == null || enemys.Count == 0)
        {
            return null;
        }

        int closestEnemyDistance = int.MaxValue;
        Position closestEnemyPosition = new Position();
        foreach (EnemySnapshot enemy in enemys)
        {
            if (Body.GetDistance(enemy.Position) < closestEnemyDistance)
            {
                closestEnemyPosition = enemy.Position;
            }

        }

        return closestEnemyPosition;
    }

    /// Determines and returns the closest explosive barrel to a specified enemy position from a given list of barrels.
    /// <param name="explosiveBarrelsList">List of positions representing the explosive barrels in the environment.</param> <param name="closestEnemyPosition">The position of the target enemy to compare distances against.</param> <return>The position of the closest explosive barrel to the specified enemy.</return>
    /// /
    public Position getClosestBarrelToEnemy(List<Position> explosiveBarrelsList, Position closestEnemyPosition)
    {
        if (explosiveBarrelsList == null || explosiveBarrelsList.Count == 0 || closestEnemyPosition == null)
        {
            return null;
        }

        int shortestDistance = int.MaxValue;
        Position positionDerKürzestenDistanz = new Position();
        
        foreach (Position barrel in explosiveBarrelsList)
        {
            if (shortestDistance > Body.GetDistance(barrel))
            {
                shortestDistance = (int) getDistance(barrel, closestEnemyPosition);
                positionDerKürzestenDistanz = barrel;
            }
        }
        
        return positionDerKürzestenDistanz;
    }

    /// Methode zur Berechnung der Manhattan-Distanz
    private double getDistance(Position position1, Position position2)
    {
        return Math.Abs(position2.X - position1.X) + Math.Abs(position2.Y - position1.Y);
    }

    private void decideMove(List<EnemySnapshot> enemys) //TODO: anstatt als Parameter zu übergeben, durch Map Speichern
    {
        //die 2 Flaggen holen
        List<FlagSnapshot> flags = Body.ExploreFlags2();
        if (flags.IsEmpty())
            return;
        FlagSnapshot ownFlag = flags.FirstOrDefault(f => f.Team == Body.Color);
        FlagSnapshot enemyFlag = flags.FirstOrDefault(f => f.Team != Body.Color);
        
        if (Body.CarryingFlag)
        {
            Position home = Body.ExploreOwnFlagStand();
            if (enemys != null && !enemys.IsEmpty())
            {
                Body.GoTo(home);
                Body.ChangeStance2(Stance.Lying);
            }
            else
            {
                Body.GoTo(home);
            }

            return;
        }
        
        //Wenn die eigene Flagge gerade vom gegner getragen wird
        if (!ownFlag.Position.Equals(Body.ExploreOwnFlagStand()))
        {
            if (Body.Stance != Stance.Standing) Body.ChangeStance2(Stance.Standing); //TODO: hinuzgefügt ungleich den rules.txt
            if(nearestToFlag(enemyFlag.Position))     //(Body.GetDistance(enemyFlag.Position) <= 10)
            {
                Body.GoTo(enemyFlag.Position);
            }
            else
            {
                Body.GoTo(ownFlag.Position);
            }
        }
        else
        {
            if (Body.Stance != Stance.Standing && Body.ActionPoints >= 2)
                Body.ChangeStance2(Stance.Standing);
            Body.GoTo(enemyFlag.Position);
            if (Body.ActionPoints >= 2)
                Body.ChangeStance2(Stance.Lying);
        }

        
    }

    private bool nearestToFlag(Position enemyFlag)
    {
        List<FriendSnapshot> team = Body.ExploreTeam();
        foreach (FriendSnapshot friend in team)
        {
            if (getDistance(friend.Position, enemyFlag) <= getDistance(Body.Position, enemyFlag))
                return false;
        }

        return true;
    }

    /// Utilizes any remaining action points to perform tasks such as reloading and exploring the environment
    /// for hills, ditches, and barriers. Communicates the discovered positions to the team.
    /// /
    private void useUnusedPoints()
    {
        Body.Reload3();
        List<Position> hills = Body.ExploreHills1();
        List<Position> ditches = Body.ExploreDitches1();
        List<Position> barrels = Body.ExploreBarrels1();

        TeamCommuniation teamCom = TeamCommuniation.Instance;
        
        if (hills != null && !hills.IsEmpty())
        {
            foreach (var hillPosition in hills)
            {
                teamCom._HillPositions.Add(hillPosition); //TODO: Das hinzufügen so richtig?
            }
        }

        if (ditches != null && !ditches.IsEmpty())
        {
            foreach (var ditchPosition in ditches)
            {
                teamCom._DitchPositions.Add(ditchPosition); //TODO: Das hinzufügen so richtig?
            }
        }

        if (barrels != null && !barrels.IsEmpty())
        {
            foreach (var barrierPosition in barrels)
            {
                teamCom._barrierPositions.Add(barrierPosition); //TODO: Das hinzufügen so richtig?
            }
        }
        
        

    }

}