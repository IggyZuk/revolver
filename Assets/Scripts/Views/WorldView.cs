using UnityEngine;
using System.Collections.Generic;

public class WorldView
{
    public GameObject root;
    public PlayerView playerView;
    public Dictionary<int, BulletView> bulletViews = new Dictionary<int, BulletView>();
    public Dictionary<int, BanditView> banditViews = new Dictionary<int, BanditView>();
    public GameObject predictionRoot;
    public GameObject inputRoot;
}
