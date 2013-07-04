﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Reversals.Backtests;
using Reversals.Backtests.Enums;
using Reversals.DataContainer;

namespace Reversals.Strategies
{
    class StepChange : Strategy
    {
        #region MAIN PARAMETERS

        private double _TV;
        private double _commission;
        private int _multiplier;
        private int _contractSize;
        private double _pointValue;
        public double _ZIM;
        private double _tickSize;
        private double _step1;
        private double _step2;

        #endregion

        #region VARIABLES
        private double _fixedPrice;
        private double _startPrice;
        private double _lastOrderPrice;
        private int _positionsNumber;
        private Operation _lastOrderOperation;
        private double _lastFridayOrderPrice;
        private Operation _lastFridayOperation;
        #endregion

        public enum AdditionalParametersEnum
        {
            TV,
            Commission,
            Multiplier,
            ContractSize,
            PointValue,
            ZIM,
            MinTick,
            Step1,
            Step2
        }

        public StepChange()
        {
            ticks = new List<Tick>();
            parameters = new StrategyParameters();
        }

        public StepChange(StrategyParameters _parameters, List<StrategyAdditionalParameter> _additional, ref Data _data)
        {
            parameters = _parameters;
            additional = _additional;
            ticks = _data.Bars;
        }

        public override void Start(int startBarIndex)
        {
            InitializeMainParameters();

            int _isExistDayTrade = 0;
            
            if (ticks.Count == 0) 
                return;
            double percent = ticks.Count/100;
            double current_tick = 0;
               
            for (int i = startBarIndex; i < ticks.Count; i++)
            {
                if (i >= current_tick)
                {
                    NotifyProgress();
                    current_tick += percent;
                }
                                  
                Tick tick = ticks[i];

                if (i == startBarIndex)
                {
                    Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                    Log("|                         NEW TEST                      |");
                    Log("|step 1 = {0}                step 2 = {1}               |", _step1, _step2);
                    Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                }

                Tick prevTick = null;
                if (i - 1 >= startBarIndex)
                    prevTick = ticks[i - 1];
                else
                {
                    Log("                                                         ");
                    Log("                          NEW DAY                        ");
                    Log("                                                         ");
                    _startPrice = tick.Close;
                    _fixedPrice = tick.Close;
                    //AddPremium(bar.Time);
                    _isExistDayTrade = trades.Count;
                    continue;
                }

                bool firstTickAfter17 = false;
                switch (tick.Time.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                    case DayOfWeek.Saturday:
                        {
                            firstTickAfter17 = false;
                            break;
                        }
                    case DayOfWeek.Monday:
                    case DayOfWeek.Tuesday:
                    case DayOfWeek.Wednesday:
                    case DayOfWeek.Thursday:
                    case DayOfWeek.Friday:
                        {
                            if (tick.Time.DayOfWeek == prevTick.Time.DayOfWeek)
                            {
                                if (tick.IntradayIndex >= 170000 && prevTick.IntradayIndex < 170000)
                                    firstTickAfter17 = true;
                                else
                                {
                                    firstTickAfter17 = false;
                                }
                                    
                            }
                            else
                            {
                                double _dayDiff = (tick.Time.Date - prevTick.Time.Date).TotalDays;
                                if (_dayDiff > 1)
                                {
                                    if (tick.Time.DayOfWeek == DayOfWeek.Monday && tick.IntradayIndex < 170000)
                                        firstTickAfter17 = false;
                                    else
                                        firstTickAfter17 = true;
                                   
                                    // GAP
                                    if (_lastFridayOperation == Operation.Buy && Math.Round(tick.Close - _fixedPrice, 2) < 0)
                                    {
                                        int contract = (int)(Math.Abs(tick.Close - _fixedPrice) / Math.Round(_step2, 2));
                                        if (contract >= 1)
                                        {
                                            contract = 1;
                                            _fixedPrice -= Math.Round(_step2, 2);
                                            contract += (int)(Math.Abs(tick.Close - _fixedPrice) / Math.Round(_step1, 2));
                                            if (contract > 1)
                                            {
                                                _fixedPrice -= (contract - 1) * Math.Round(_step1, 2);
                                                CreateOrder(Operation.Sell, tick, true, positions.Count + contract);
                                            }
                                        }
                                    }
                                    else 
                                        if (_lastFridayOperation == Operation.Sell && Math.Round(_fixedPrice - tick.Close, 2) < 0)
                                        {
                                            int contract = (int)(Math.Abs(tick.Close - _fixedPrice) / Math.Round(_step2, 2));
                                            if (contract >= 1)
                                            {
                                                contract = 1;
                                                _fixedPrice += Math.Round(_step2, 2);
                                                contract += (int)(Math.Abs(tick.Close - _fixedPrice) / Math.Round(_step1, 2));
                                                if (contract > 1)
                                                {
                                                    _fixedPrice += (contract - 1) * Math.Round(_step1, 2);
                                                    CreateOrder(Operation.Buy, tick, true, positions.Count + contract);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int _contract = (int)(Math.Abs(tick.Close - _fixedPrice) / Math.Round(_step1, 2));
                                            if (_contract > 1)
                                            {
                                                if (tick.Close > _fixedPrice)
                                                {
                                                    _fixedPrice += _contract * Math.Round(_step1, 2);
                                                    CreateOrder(Operation.Buy, tick, false, _contract);

                                                }
                                                else
                                                {
                                                    _fixedPrice -= _contract * Math.Round(_step1, 2);
                                                    CreateOrder(Operation.Sell, tick, false, _contract);
                                                }
                                                DispatchOrders(tick);
                                            }
                                        }
                                }
                                else
                                {
                                    if (tick.IntradayIndex < 170000)
                                        firstTickAfter17 = false;
                                    else
                                        firstTickAfter17 = true;
                                }
                            }
                            break;
                        }
                }

                if (firstTickAfter17)
                {
                    // END TRADING DAY                    
                    Trade(ref positions, tick);
                    positions.Clear();
                    orders.Clear();
                    _isExistDayTrade = trades.Count - _isExistDayTrade;

                    //PosPNL 
                    if (_isExistDayTrade > 0)
                    {
                        double _dayChangedPrice = Math.Pow(tick.Close - _startPrice, 2)*_contractSize*_multiplier*_pointValue/2;

                        double _lastOrderPriceChange = 0.0;
                        if (_lastOrderPrice != 0.0)
                        {
                            _lastOrderPriceChange = Math.Pow(tick.Close - _lastOrderPrice, 2)*_contractSize*_multiplier*
                                                    _pointValue/2;
                        }

                        double _summaryChangedProfit = _dayChangedPrice + _lastOrderPriceChange;

                        AddEndDayPnl(_isExistDayTrade, _summaryChangedProfit, _lastOrderPriceChange, _dayChangedPrice, tick);
                    }
                    else
                    {
                        AddEndDayPnl(0, 0, 0, 0, tick);
                    }


                    // START TRADING DAY
                    //AddPremium(bar.Time);

                    _lastFridayOrderPrice = 0.0;
                    _lastOrderPrice = 0.0;
                    _startPrice = tick.Close;
                    _fixedPrice = tick.Close;
                    _isExistDayTrade = trades.Count;
                }


                // TRADING
                if (tick.Time.DayOfWeek == DayOfWeek.Saturday || tick.Time.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                
                if (_lastOrderOperation == Operation.Buy && positions.Count != 0 && Math.Round(tick.Close - _fixedPrice, 2) < 0 && Math.Round(_fixedPrice - tick.Close, 2) >= Math.Round(_step2, 2))
                {
                    _fixedPrice -= Math.Round(_step2, 2);
                    
                    CreateOrder(Operation.Sell, tick, true, positions.Count + 1);
                    if (tick.Time.DayOfWeek == DayOfWeek.Friday && tick.IntradayIndex >= 170000)
                    {
                        _lastFridayOrderPrice = _fixedPrice;
                        _lastFridayOperation = Operation.Sell;
                    }
                }
                else if (_lastOrderOperation == Operation.Sell && positions.Count != 0 && Math.Round(_fixedPrice - tick.Close, 2) < 0 && Math.Round(tick.Close - _fixedPrice, 2) >= Math.Round(_step2, 2))
                {
                    _fixedPrice += Math.Round(_step2, 2);

                    CreateOrder(Operation.Buy, tick, true, positions.Count + 1);
                    if (tick.Time.DayOfWeek == DayOfWeek.Friday && tick.IntradayIndex >= 170000)
                    {
                        _lastFridayOrderPrice = _fixedPrice;
                        _lastFridayOperation = Operation.Buy;
                    }
                }
                else
                {
                    if (Math.Round(tick.Close - _fixedPrice, 2) >= Math.Round(_step1, 2) && _lastOrderOperation != Operation.Sell)
                    {
                        CreateOrder(Operation.Buy, tick, false, 1);
                        if (tick.Time.DayOfWeek == DayOfWeek.Friday && tick.IntradayIndex >= 170000)
                        {
                            _lastFridayOrderPrice = _fixedPrice;
                            _lastFridayOperation = Operation.Buy;
                        }
                    }
                    else if (Math.Round(_fixedPrice - tick.Close, 2) >= Math.Round(_step1, 2) && _lastOrderOperation != Operation.Buy)
                    {
                        CreateOrder(Operation.Sell, tick, false, 1);
                        if (tick.Time.DayOfWeek == DayOfWeek.Friday && tick.IntradayIndex >= 170000)
                        {
                            _lastFridayOrderPrice = _fixedPrice;
                            _lastFridayOperation = Operation.Sell;
                        }
                    }
                }

                DispatchOrders(tick);
            }

            _isExistDayTrade = trades.Count - _isExistDayTrade;
            if (trades[trades.Count - 1].Operation != Operation.PNL)
            {
                if (_isExistDayTrade > 0)
                {
                    double _dayChangedPrice = Math.Pow(ticks[ticks.Count-1].Close - _startPrice, 2) * _contractSize * _multiplier * _pointValue / 2;

                    double _lastOrderPriceChange = 0.0;
                    if (_lastOrderPrice != 0.0)
                    {
                        _lastOrderPriceChange = Math.Pow(ticks[ticks.Count - 1].Close - _lastOrderPrice, 2) * _contractSize * _multiplier *
                                                _pointValue / 2;
                    }

                    double _summaryChangedProfit = _dayChangedPrice + _lastOrderPriceChange;

                    //AddEndDayPnl(_isExistDayTrade, _summaryChangedProfit, _lastOrderPriceChange, _dayChangedPrice, bars[bars.Count - 1]);

                    Position endDayPNL = new Position(parameters.Account, parameters.Symbol, -1, ticks[ticks.Count - 1].Time,
                      Operation.PNL, 1, _startPrice, ticks[ticks.Count - 1].Close, "", "", true);
                    double _dailyCommision = -_isExistDayTrade * _commission * _contractSize * _ZIM;
                    endDayPNL.TimeClose = ticks[ticks.Count - 1].Time;
                    endDayPNL.Trades = _summaryChangedProfit; //Trades
                    endDayPNL.Commission = _dailyCommision;
                    endDayPNL.PosPNL = _dayChangedPrice;
                    endDayPNL.ClosePNL = _lastOrderPriceChange;
                    trades.Add(endDayPNL);
                }
                else
                {
                    AddEndDayPnl(0, 0, 0, 0, ticks[ticks.Count - 1]);
                }
            }

            if (ticks[0].IntradayIndex < 170000)
            {
                Tick tick = ticks[0];
                AddPremium(new DateTime(tick.Time.Year, tick.Time.Month, tick.Time.Day, 17, 00, 00));
            }
            DateTime _premiumTime = new DateTime(ticks[0].Time.Year,
                                                ticks[0].Time.Month,
                                                ticks[0].Time.AddDays(1).Day,//bars[0].IntradayIndex < 170000 ? bars[0].Time.AddDays(-1).Day : bars[0].Time.Day,
                                                17, 0, 0);
            while (_premiumTime <= ticks[ticks.Count - 1].Time)
            {
                AddPremium(_premiumTime);
                _premiumTime = _premiumTime.AddDays(1);
            }

            if (ticks[ticks.Count - 1].IntradayIndex < 170000 && ticks[ticks.Count - 1].Time.Day > trades[trades.Count - 1].TimeOpen.Day)
            {
                Tick tick = ticks[ticks.Count - 1];
                AddPremium(new DateTime(tick.Time.Year, tick.Time.Month, tick.Time.Day, 17, 00, 00));
            }

            if (ticks[ticks.Count - 1].IntradayIndex > 170000 && ticks[ticks.Count - 1].Time.Day == trades[trades.Count - 1].TimeOpen.Day)
            {
                Tick tick = ticks[ticks.Count - 1];
                AddPremium(new DateTime(tick.Time.Year, tick.Time.Month, tick.Time.Day+1, 17, 00, 00));
            }

            for (int i = 0; i < trades.Count; i++)
            {
                if (!trades[i].IsPremium)
                {
                    trades[i].Trades *= -1 * _ZIM * _contractSize;
                }
            }
           
            Log("{0}", trades.Count);

        }

        private void AddPremium(DateTime open)
        {
            Position startHolidayPremium = new Position(parameters.Account, parameters.Symbol, -1, open, Operation.Premium, 1, 0, 0, "", "", true);
            startHolidayPremium.Trades = _TV;
            startHolidayPremium.TimeClose = open;
            trades.Add(startHolidayPremium);
            Log("{0}    -    was added premium = {1}", startHolidayPremium.TimeOpen, _TV);
        }

        private void CreateOrder(Operation operation, Tick tick, bool isReverse, int size)
        {
            _lastOrderOperation = operation;
            if (isReverse && size == 1)
            {
                Log("{0}    -    fixed price {1} - bar close {2} = {3} REVERSE", tick.Time, _fixedPrice, tick.Close, Math.Round(_fixedPrice - tick.Close, 2));
                //if (operation == Operation.Buy)
                //{
                //    _fixedPrice += _step2;
                //}
                //else if (operation == Operation.Sell)
                //{
                //    _fixedPrice -= _step2;
                //}
            }
            else 
            if (size == 1)
            {                
                Log("{0}    -    fixed price {1} - bar close {2} = {3}", tick.Time, _fixedPrice, tick.Close, Math.Round(_fixedPrice - tick.Close, 2));
                if (operation == Operation.Buy)
                {
                    _fixedPrice += Math.Round(_step1, 2);
                }
                else if (operation == Operation.Sell)
                {
                    _fixedPrice -= Math.Round(_step1, 2);
                }
            }

            //_fixedPrice = bar.Close;
            order_id++;

            if (operation == Operation.Buy)
            {
                _positionsNumber++;
            }
            else
            {
                _positionsNumber--;
            }
            
            orders.Add(new Order(parameters.Account, parameters.Symbol, order_id, tick.Time, operation,
                                 OrderType.Market, size, tick.Close, "", ""));
            _lastOrderPrice = tick.Close;
            Log("{4}    -    order id {0} added op {1} at price {2} with posNumber {3}, range {5}", order_id, operation, tick.Close, _positionsNumber, tick.Time, tick.Close - _fixedPrice);
        }

        private void InitializeMainParameters ()
        {
            _TV = Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.TV).Value, new CultureInfo("en-US", false));
            _commission = -Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.Commission).Value, new CultureInfo("en-US", false));
            _multiplier = Convert.ToInt32(AdditionalParameter(AdditionalParametersEnum.Multiplier).Value);
            _contractSize = Convert.ToInt32(AdditionalParameter(AdditionalParametersEnum.ContractSize).Value);
            _tickSize = Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.MinTick).Value, new CultureInfo("en-US", false));
            _step1 = Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.Step1).Value, new CultureInfo("en-US", false)); // *_tickSize;
            _step2 = Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.Step2).Value, new CultureInfo("en-US", false)); // *_tickSize;
            _pointValue = Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.PointValue).Value, new CultureInfo("en-US", false));

            //additional[5] = new Strategy.StrategyAdditionalParameter("", "ZIM", _step1 * _pointValue,typeof (double),false);
            if (IsOptimisation == false)
            {
                _ZIM = Convert.ToDouble(AdditionalParameter(AdditionalParametersEnum.ZIM).Value,
                                        new CultureInfo("en-US", false));
            }
            else
            {
                _ZIM = _pointValue*_step1;
                additional[5] = new StrategyAdditionalParameter("", "ZIM", _step1 * _pointValue,typeof (double),false);
            }
            //_step1*_pointValue/_tickSize;
        }

        private void AddEndDayPnl(int isExistDayTrade, double summaryChangedProfit, double lastOrderPriceChange, double dayChangedPrice, Tick tick)
        {

            DateTime temp = new DateTime(tick.Time.Year, tick.Time.Month, tick.Time.Day, 17, 0, 0);
            if (temp > tick.Time) temp = temp.AddDays(-1);
            Position endDayPNL = new Position(parameters.Account, parameters.Symbol, -1, temp,
                                  Operation.PNL, 1, _startPrice, tick.Close, "", "", true);
            double _dailyCommision = -isExistDayTrade * _commission * _contractSize * _ZIM;
            endDayPNL.TimeClose = tick.Time;
            endDayPNL.Trades = summaryChangedProfit; //Trades
            endDayPNL.Commission = _dailyCommision;
            endDayPNL.PosPNL = dayChangedPrice;
            endDayPNL.ClosePNL = lastOrderPriceChange;
            trades.Add(endDayPNL);
            
            /*
             * 
             *  _lastOrderPriceChange = (Math.Pow(bar.Close - _lastOrderPrice, 2)*_contractSize*_multiplier*
                                                    _pointValue)/2;
                    }

                    _summaryChangedProfit = _dayChangedPrice + _lastOrderPriceChange;
             * */
            Log("{0}    -    Was added summary profit - summary profit {1} = summarySqrValue ( {4} + {5}) - commission {3}", tick.Time, Math.Round(endDayPNL.Trades, 2), Math.Round(summaryChangedProfit, 2), Math.Round(endDayPNL.Commission, 2), dayChangedPrice, lastOrderPriceChange);
        }

        public override void Trade(ref List<Position> _positions, Tick tick)
        {
            for (int i = 0; i < _positions.Count; i++)
            {              
                Position _position = _positions[i];
                order_id++;
                _position.Comment = "Closed for Exit";
                if (_positionsNumber > 0)
                {
                    _positionsNumber--;
                }
                else if (_positionsNumber<0)
                {
                    _positionsNumber++;
                }
                Log("{0}    -    position closed by order id {4} with price {1} last positions are {2} with fpl {3}", tick.Time, _position.Price, _positionsNumber, Math.Round(-_position.Trades * _ZIM * _contractSize, 2), order_id, _position.TimeOpen);
                _position.TimeClose = tick.Time;
                _position.Last = tick.Close;
                _position.CloseOrderId = order_id;
                trades.Add(_position);
            }
        }

        public StrategyAdditionalParameter AdditionalParameter(AdditionalParametersEnum parameter)
        {
            switch (parameter)
            {
                case AdditionalParametersEnum.TV:
                    {
                        return (from item in additional where (item.Name == "Time Value") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.Commission:
                    {
                        return (from item in additional where (item.Name == "Commission") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.Multiplier:
                    {
                        return (from item in additional where (item.Name == "Multiplier") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.ContractSize:
                    {
                        return (from item in additional where (item.Name == "Contract Size") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.PointValue:
                    {
                        return (from item in additional where (item.Name == "Point Value") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.ZIM:
                    {
                        return (from item in additional where (item.Name == "ZIM") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.MinTick:
                    {
                        return (from item in additional where (item.Name == "Tick Size") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.Step1:
                    {
                        return (from item in additional where (item.Name == "Stop Level") select item).ElementAt(0);
                    }
                case AdditionalParametersEnum.Step2:
                    {
                        return (from item in additional where (item.Name == "Reversal Level") select item).ElementAt(0);
                    }
                default:
                    {
                        throw new ArgumentException("Unknown AdditionalParameter in EmaForMarket");
                    }
            }
        }

        public override void Clear()
        {
            backtest = null;
            orders.Clear();
            positions.Clear();
            trades.Clear();
        }
    }
}