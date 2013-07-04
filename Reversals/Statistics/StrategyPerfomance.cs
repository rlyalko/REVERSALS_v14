using System;
using System.Collections.Generic;
using Reversals.Backtests;
using Reversals.Backtests.Enums;

namespace Reversals.Statistics
{
    public class StrategyPerfomance
    {
        private int _entireTradesCount;
        private int _lossTradesCount;
        private int _profitTradesCount;
        private int _zeroTradesCount;

        private double _entireTradesSum;
        
        private readonly double _lossTradesSum;
        private readonly double _profitTradesSum;
        private readonly double _sharpe;
         
        private readonly double _profitability;
        private readonly double _profitFactor;
        private readonly double _averageTrade;

        private int _reversals;

        

        public StrategyPerfomance(List<Position> data, double tickSize, bool isIntraday, DateTime startForSharpe, DateTime endForSharpe)
        {
            CalculateTradesCount(data, tickSize);
            CalculateTradesSum(data, tickSize, isIntraday);

            _profitability = (_entireTradesCount != 0) ? (100 * _profitTradesCount) / _entireTradesCount : 0.0;
            _profitFactor = (_lossTradesSum != 0) ? (-_profitTradesSum / _lossTradesSum) : 0.0;
            _averageTrade = (_entireTradesCount != 0) ? (_entireTradesSum / _entireTradesCount) : 0;

            //CalculateSharpe(data, tickSize, isIntraday, startForSharpe, endForSharpe);
        }

        private void CalculateTradesCount(List<Position> data, double tickSize)
        {
            int etc = 0;
            int ltc = 0;
            int ptc = 0;
            int ztc = 0;

            for (int i = 0; i < data.Count; i++)
            {
                Position position = data[i];
                if (position.Trades > 0) ptc++;
                if (position.Trades < 0) ltc++;
                if (position.Trades == 0) ztc++;
            }

            etc = ptc + ltc;

            _entireTradesCount = etc;
            _lossTradesCount = ltc;
            _profitTradesCount = ptc;
            _zeroTradesCount = ztc;
        }

        private void CalculateTradesSum(List<Position> data, double tickSize, bool isIntraday)
        {
            double ets = 0;
            _reversals = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var position = data[i];
                //if (isIntraday)
                //{

                if (position.Operation != Operation.PNL)
                {
                    ets += position.Trades;
                }
                else if (position.Operation == Operation.PNL)
                {
                    ets += position.Commission + position.ClosePNL + position.PosPNL;
                }
                    
                //    if (position.Trades < 0) lts += position.Trades + position.Commission + position.ClosePNL + position.PosPNL;
                //}
                //else
                //{
                //    if (position.Trades > 0) pts += 100 * position.Trades / position.Price;
                //    if (position.Trades < 0) lts += 100 * position.Trades / position.Price;
                //}

                if (!position.IsPremium && position.Comment != "Closed for Exit")
                {
                    _reversals++;
                }
            }

            //ets = pts + lts;

            //if (isIntraday)
            //{
            //    entireTradesSum = ets / tickSize;
            //    lossTradesSum = lts / tickSize;
            //    profitTradesSum = pts / tickSize;
            //}
            //else
            //{
                _entireTradesSum = ets;
                //lossTradesSum = lts;
                //profitTradesSum = pts;
            //}
        }

       

        public int EntireTradesCount
        {
            get { return _entireTradesCount; }
        }

        public int Reversals
        {
            get { return _reversals; }
        }

        public int LossTradesCount
        {
            get { return _lossTradesCount; }
        }

        public int ProfitTradesCount
        {
            get { return _profitTradesCount; }
        }

        public int ZerotradesCount
        {
            get { return _zeroTradesCount; }
        }

        public double EntireTradesSum
        {
            get { return _entireTradesSum; }
        }

        public double LossTradesSum
        {
            get { return _lossTradesSum; }
        }

        public double ProfitTradesSum
        {
            get { return _profitTradesSum; }
        }

        public double Profitability
        {
            get { return _profitability; }
        }

        public double ProfitFactor
        {
            get { return _profitFactor; }
        }

        public double AverageTrade
        {
            get { return _averageTrade; }
        }

        public double Sharpe
        {
            get { return _sharpe; }
        }
    }
}
