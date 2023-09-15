﻿using System;
using System.Collections.Generic;

namespace HintMachine.Games
{
    public class TetrisEffectConnector : IGameConnectorProcess
    {
        private uint _previousScore = uint.MaxValue;
        private readonly HintQuest _scoreQuest = new HintQuest("Score", 20000);

        public TetrisEffectConnector() : base("TetrisEffect-Win64-Shipping")
        {
            quests.Add(_scoreQuest);
        }

        public override string GetDisplayName()
        {
            return "Tetris Effect Connected (Steam)";
        }
        public override string GetDescription()
        {
            return "Stack tetrominos and fill lines to clear them in the most visually " +
                   "impressive implementation of Tetris ever made.\n\n" +
                   "Tested on up-to-date Steam version.";
        }

        public override bool Poll()
        {
            if (process == null || module == null)
                return false;

            long baseAddress = module.BaseAddress.ToInt64() + 0x4ED0440;
            long scoreAddress = ResolvePointerPath(baseAddress, new int[] { 0x0, 0x20, 0x120, 0x0, 0x42C });
            
            uint score = ReadUint32(scoreAddress);
            if (score > _previousScore)
                _scoreQuest.Add(score - _previousScore);
            _previousScore = score;

            return true;
        }
    }
}
