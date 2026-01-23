import React, { useState, useEffect, useMemo } from 'react';
import { 
  CreditCard, 
  Delete, 
  CheckCircle, 
  Settings,
  Banknote,
  Coins,
  Calculator,
  Trash2,
  Smartphone,
  QrCode,
  Wifi,
  Sun,
  Moon
} from 'lucide-react';

const POSPayment = () => {
  // --- Configuration & Data ---
  
  const currencies = {
    THB: {
      code: 'THB',
      symbol: '฿',
      name: 'Thai Baht',
      rate: 1, 
      denominations: [1000, 500, 100, 50, 20, 10, 5, 2, 1],
      flagCode: 'th' 
    },
    USD: {
      code: 'USD',
      symbol: '$',
      name: 'US Dollar',
      rate: 35.50,
      denominations: [100, 50, 20, 10, 5, 1],
      flagCode: 'us'
    },
    GBP: {
      code: 'GBP',
      symbol: '£',
      name: 'British Pound',
      rate: 45.20,
      denominations: [50, 20, 10, 5, 2, 1],
      flagCode: 'gb'
    },
    EUR: {
      code: 'EUR',
      symbol: '€',
      name: 'Euro',
      rate: 38.80,
      denominations: [200, 100, 50, 20, 10, 5, 2, 1],
      flagCode: 'eu'
    },
    CNY: {
      code: 'CNY',
      symbol: '¥',
      name: 'Chinese Yuan',
      rate: 4.95,
      denominations: [100, 50, 20, 10, 5, 1],
      flagCode: 'cn'
    }
  };

  const paymentMethods = [
    { id: 'cash', label: 'Cash', icon: Banknote, color: 'blue' },
    { id: 'card', label: 'Credit Card', icon: CreditCard, color: 'indigo' },
    { id: 'promptpay', label: 'PromptPay', icon: QrCode, color: 'sky' },
    { id: 'alipay', label: 'AliPay', icon: Smartphone, color: 'cyan' },
  ];

  // --- State ---
  const [billAmount, setBillAmount] = useState(1500); 
  const [activeMethod, setActiveMethod] = useState('cash'); // 'cash', 'card', 'promptpay', 'alipay'
  const [selectedCurrency, setSelectedCurrency] = useState('THB');
  const [isPaymentComplete, setIsPaymentComplete] = useState(false);
  const [isDarkMode, setIsDarkMode] = useState(false); // Theme State

  // MIXED PAYMENT STATE
  const [thbAmount, setThbAmount] = useState(0); // Cash THB
  const [foreignCounts, setForeignCounts] = useState({}); // Cash Foreign
  const [nonCashPayments, setNonCashPayments] = useState([]); // [{ id, type, amount, label }]

  // Input state for non-cash methods
  const [digitalAmountInput, setDigitalAmountInput] = useState('');

  // --- Calculations ---

  const getForeignTotal = (currencyCode, counts = {}) => {
    if (!counts) return 0;
    return Object.entries(counts).reduce((total, [denom, count]) => {
      return total + (parseFloat(denom) * count);
    }, 0);
  };

  const grandTotalTHB = useMemo(() => {
    let total = thbAmount; 

    // Add foreign cash
    Object.keys(foreignCounts).forEach(code => {
      const currencyTotal = getForeignTotal(code, foreignCounts[code]);
      const rate = currencies[code].rate;
      total += currencyTotal * rate;
    });

    // Add non-cash payments
    nonCashPayments.forEach(p => {
      total += p.amount;
    });

    return total;
  }, [thbAmount, foreignCounts, nonCashPayments]);

  const changeDue = grandTotalTHB - billAmount;
  const isSufficient = grandTotalTHB >= billAmount - 0.5;
  const remainingDue = Math.max(0, billAmount - grandTotalTHB);

  // Auto-fill digital input when switching tabs
  useEffect(() => {
    if (activeMethod !== 'cash') {
      setDigitalAmountInput(remainingDue > 0 ? remainingDue.toString() : '');
    }
  }, [activeMethod, billAmount, grandTotalTHB]); 

  // --- Handlers ---

  const handleClearAll = () => {
    setThbAmount(0);
    setForeignCounts({});
    setNonCashPayments([]);
    setIsPaymentComplete(false);
    setActiveMethod('cash');
  };

  const handleClearCurrent = () => {
    if (activeMethod === 'cash') {
      if (selectedCurrency === 'THB') {
        setThbAmount(0);
      } else {
        setForeignCounts(prev => {
          const newState = { ...prev };
          delete newState[selectedCurrency];
          return newState;
        });
      }
    } else {
      setDigitalAmountInput('');
    }
  };

  const handleRemovePayment = (id) => {
    setNonCashPayments(prev => prev.filter(p => p.id !== id));
  };

  const handleCompletePayment = () => {
    if (isSufficient) {
      setIsPaymentComplete(true);
    }
  };

  const handleAddDigitalPayment = () => {
    const val = parseFloat(digitalAmountInput);
    if (!val || val <= 0) return;

    const newPayment = {
      id: Date.now(),
      type: activeMethod,
      label: paymentMethods.find(m => m.id === activeMethod)?.label,
      amount: val
    };

    setNonCashPayments(prev => [...prev, newPayment]);
    setDigitalAmountInput('');
  };

  // Handler for THB Numpad
  const handleTHBNumpad = (val) => {
    if (isPaymentComplete) return;
    
    setThbAmount(prev => {
      const currentString = prev.toString();
      if (val === 'C') return 0;
      if (val === 'BS') return Math.floor(prev / 10);
      
      let newString = currentString === '0' ? val.toString() : currentString + val.toString();
      return parseFloat(newString);
    });
  };

  // Handler for Foreign Note Counts
  const handleCountChange = (denom, val) => {
    if (isPaymentComplete) return;
    const count = parseInt(val) || 0;
    
    setForeignCounts(prev => ({
      ...prev,
      [selectedCurrency]: {
        ...(prev[selectedCurrency] || {}),
        [denom]: count
      }
    }));
  };

  const formatMoney = (amount, currencyCode, showSymbol = true) => {
    const symbol = showSymbol ? (currencies[currencyCode]?.symbol || '') : '';
    return `${symbol}${amount.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
  };

  // Get active payments list for summary
  const activePayments = useMemo(() => {
    const payments = [];
    
    // 1. Cash THB
    if (thbAmount > 0) {
      payments.push({
        id: 'thb-cash',
        label: 'Cash (THB)',
        code: 'THB',
        amount: thbAmount,
        rate: 1,
        thbValue: thbAmount,
        isCash: true
      });
    }

    // 2. Cash Foreign
    Object.keys(foreignCounts).forEach(code => {
      const total = getForeignTotal(code, foreignCounts[code]);
      if (total > 0) {
        payments.push({
          id: `cash-${code}`,
          label: `Cash (${code})`,
          code: code,
          amount: total,
          rate: currencies[code].rate,
          thbValue: total * currencies[code].rate,
          isCash: true
        });
      }
    });

    // 3. Non-Cash
    nonCashPayments.forEach(p => {
      payments.push({
        ...p,
        code: 'THB', 
        thbValue: p.amount,
        rate: 1,
        isCash: false
      });
    });

    return payments;
  }, [thbAmount, foreignCounts, nonCashPayments]);

  return (
    <div className={`min-h-screen transition-colors duration-300 ${isDarkMode ? 'dark bg-gray-900' : 'bg-gray-100'} p-4 md:p-8 font-sans text-gray-800`}>
      
      {/* Header */}
      <div className="max-w-6xl mx-auto mb-6 flex justify-between items-center">
        <div>
          <h1 className={`text-2xl font-bold ${isDarkMode ? 'text-white' : 'text-gray-900'}`}>Payment Terminal</h1>
          <p className={`${isDarkMode ? 'text-gray-400' : 'text-gray-500'} text-sm`}>Transaction #882391 • Mixed Payment Mode</p>
        </div>
        <div className="flex items-center gap-3">
          <button 
            onClick={() => setIsDarkMode(!isDarkMode)}
            className={`p-2 rounded-lg transition-colors ${isDarkMode ? 'bg-gray-800 text-yellow-400 hover:bg-gray-700' : 'bg-white text-gray-600 hover:bg-gray-100'} shadow-sm border ${isDarkMode ? 'border-gray-700' : 'border-gray-200'}`}
          >
            {isDarkMode ? <Sun size={20} /> : <Moon size={20} />}
          </button>
          <div className={`${isDarkMode ? 'bg-gray-800 border-gray-700' : 'bg-white border-gray-200'} px-4 py-2 rounded-lg shadow-sm border flex items-center gap-2`}>
            <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse"></div>
            <span className={`text-sm font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}>System Online</span>
          </div>
        </div>
      </div>

      <div className="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-12 gap-6">
        
        {/* LEFT COLUMN: Bill Details & Payment Summary */}
        <div className="lg:col-span-4 space-y-4">
          
          {/* Bill Total Card */}
          <div className={`${isDarkMode ? 'bg-gray-800 border-gray-700' : 'bg-white border-gray-200'} rounded-2xl shadow-sm border p-6`}>
            <label className={`block text-sm font-medium ${isDarkMode ? 'text-gray-400' : 'text-gray-500'} mb-1`}>Total Amount Due (THB)</label>
            <div className="flex items-center justify-between mb-4">
              <span className={`text-4xl font-bold ${isDarkMode ? 'text-white' : 'text-gray-900'}`}>฿{billAmount.toLocaleString()}</span>
              <button className={`p-2 rounded-full ${isDarkMode ? 'hover:bg-gray-700 text-gray-500' : 'hover:bg-gray-100 text-gray-400'}`}>
                <Settings size={20} />
              </button>
            </div>
            
            <input 
              type="range" 
              min="100" 
              max="10000" 
              step="50"
              value={billAmount}
              onChange={(e) => {
                setBillAmount(parseInt(e.target.value));
                handleClearAll();
              }}
              className={`w-full h-2 rounded-lg appearance-none cursor-pointer accent-blue-600 ${isDarkMode ? 'bg-gray-700' : 'bg-gray-200'}`}
            />
          </div>

          {/* Detailed Payment Summary Card */}
          <div className={`rounded-2xl shadow-sm border p-6 transition-all duration-300 
            ${isPaymentComplete 
              ? (isDarkMode ? 'bg-green-900/20 border-green-800' : 'bg-green-50 border-green-200') 
              : (isDarkMode ? 'bg-gray-800 border-gray-700' : 'bg-white border-gray-200')
            }`}
          >
            <h3 className={`text-lg font-semibold ${isDarkMode ? 'text-gray-100' : 'text-gray-800'} mb-4 flex items-center gap-2`}>
              <CreditCard size={20} />
              Payment Details
            </h3>

            {/* Empty State */}
            {activePayments.length === 0 && (
              <div className={`text-center py-8 text-sm ${isDarkMode ? 'text-gray-500' : 'text-gray-400'}`}>
                No payment entered yet.<br/>Select a method to begin.
              </div>
            )}

            {/* List of Payments */}
            <div className="space-y-3 mb-4 max-h-60 overflow-y-auto pr-1">
              {activePayments.map((p) => (
                <div key={p.id} className={`${isDarkMode ? 'bg-gray-700/50 border-gray-600' : 'bg-gray-50 border-gray-100'} rounded-lg p-3 border relative group`}>
                  <div className="flex justify-between items-center mb-1">
                    <div className="flex items-center gap-2">
                       {p.isCash && p.code !== 'THB' && (
                         <img 
                          src={`https://flagcdn.com/20x15/${currencies[p.code].flagCode}.png`}
                          alt={p.code}
                          className="h-3 rounded-sm shadow-sm"
                        />
                       )}
                       {!p.isCash && (
                          activeMethod === 'card' ? <CreditCard size={14} className="text-indigo-500"/> : 
                          <Smartphone size={14} className="text-blue-500"/>
                       )}
                      <span className={`font-bold text-sm ${isDarkMode ? 'text-gray-200' : 'text-gray-700'}`}>{p.label || p.code}</span>
                    </div>
                    <span className={`font-bold text-lg ${isDarkMode ? 'text-gray-100' : 'text-gray-900'}`}>
                      {formatMoney(p.amount, p.code)}
                    </span>
                  </div>
                  
                  {/* Conversion Details (only for foreign cash) */}
                  {p.isCash && p.code !== 'THB' && (
                    <div className={`flex justify-between items-center text-xs ${isDarkMode ? 'text-gray-400 border-gray-600' : 'text-gray-500 border-gray-200'} border-t pt-1 mt-1`}>
                      <span>Rate: {p.rate}</span>
                      <span className={`font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}>
                         = ฿{p.thbValue.toLocaleString(undefined, {maximumFractionDigits: 0})}
                      </span>
                    </div>
                  )}

                  {/* Delete Button for Non-Cash */}
                  {!p.isCash && !isPaymentComplete && (
                    <button 
                      onClick={() => handleRemovePayment(p.id)}
                      className={`absolute -top-2 -right-2 rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity shadow-sm ${isDarkMode ? 'bg-red-900/50 text-red-400' : 'bg-red-100 text-red-600'}`}
                    >
                      <Trash2 size={12} />
                    </button>
                  )}
                </div>
              ))}
            </div>

            {/* Totals Section */}
            <div className={`border-t pt-4 ${isDarkMode ? 'border-gray-700' : 'border-gray-100'}`}>
              <div className="flex justify-between items-center py-1">
                <span className={`${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}>Total Received (THB)</span>
                <span className={`font-bold text-xl ${isSufficient ? 'text-green-500' : (isDarkMode ? 'text-white' : 'text-gray-900')}`}>
                  ฿{grandTotalTHB.toLocaleString(undefined, {maximumFractionDigits: 0})}
                </span>
              </div>

              <div className="flex justify-between items-center py-1">
                <span className={`${isDarkMode ? 'text-gray-400' : 'text-gray-500'}`}>Change (THB)</span>
                <span className={`font-bold text-xl ${isDarkMode ? 'text-white' : 'text-gray-900'}`}>
                  ฿{Math.max(0, changeDue).toLocaleString(undefined, {maximumFractionDigits: 0})}
                </span>
              </div>

              <div className="pt-4">
                 {isPaymentComplete ? (
                   <div className={`text-center mt-2 rounded-xl p-4 border ${isDarkMode ? 'bg-green-900/30 border-green-800' : 'bg-green-100 border-green-200'}`}>
                      <div className={`flex items-center justify-center mb-1 gap-2 ${isDarkMode ? 'text-green-400' : 'text-green-700'}`}>
                        <CheckCircle size={24} />
                        <span className="text-lg font-bold">Paid</span>
                      </div>
                      <div className={`text-sm mb-1 font-medium ${isDarkMode ? 'text-gray-300' : 'text-gray-600'}`}>Change Due (THB)</div>
                      <div className={`text-3xl font-bold ${isDarkMode ? 'text-white' : 'text-gray-900'}`}>฿{changeDue.toLocaleString(undefined, {maximumFractionDigits: 0})}</div>
                   </div>
                 ) : (
                   <div className="flex justify-between items-end mt-2">
                     <span className="text-red-500 font-medium text-sm">Remaining (THB)</span>
                     <span className="text-2xl font-bold text-red-500">
                        ฿{remainingDue.toLocaleString(undefined, {maximumFractionDigits: 0})}
                     </span>
                   </div>
                 )}
              </div>
            </div>

            <button 
              onClick={handleCompletePayment}
              disabled={!isSufficient || isPaymentComplete}
              className={`w-full mt-6 py-4 rounded-xl text-lg font-bold shadow-lg transition-all transform active:scale-95 ${
                isPaymentComplete 
                  ? (isDarkMode ? 'bg-gray-700 text-gray-500' : 'bg-gray-100 text-gray-400') + ' cursor-not-allowed shadow-none'
                  : isSufficient
                    ? 'bg-blue-600 text-white hover:bg-blue-700 shadow-blue-200'
                    : (isDarkMode ? 'bg-gray-700 text-gray-500' : 'bg-gray-200 text-gray-400') + ' cursor-not-allowed'
              }`}
            >
              {isPaymentComplete ? 'Transaction Completed' : 'Complete Payment'}
            </button>
            {isPaymentComplete && (
              <button onClick={handleClearAll} className={`w-full mt-3 py-3 font-medium rounded-lg ${isDarkMode ? 'text-blue-400 hover:bg-gray-700' : 'text-blue-600 hover:bg-blue-50'}`}>Start New Payment</button>
            )}
          </div>
        </div>

        {/* RIGHT COLUMN: Interaction Area */}
        <div className="lg:col-span-8 space-y-6">
          
          {/* Main Payment Method Tabs */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
             {paymentMethods.map(method => {
               const Icon = method.icon;
               const isActive = activeMethod === method.id;
               
               // Dynamic class generation based on color and theme
               let activeClass = '';
               if (isActive) {
                 if (isDarkMode) {
                   activeClass = `border-${method.color}-500 bg-${method.color}-900/30 text-${method.color}-300 shadow-sm`;
                 } else {
                   activeClass = `border-${method.color}-500 bg-${method.color}-50 text-${method.color}-700 shadow-sm`;
                 }
               } else {
                 activeClass = isDarkMode 
                    ? 'border-gray-700 bg-gray-800 text-gray-400 hover:border-gray-600 hover:bg-gray-700' 
                    : 'border-gray-200 bg-white text-gray-500 hover:border-gray-300 hover:bg-gray-50';
               }

               return (
                 <button
                   key={method.id}
                   onClick={() => setActiveMethod(method.id)}
                   className={`
                     flex flex-col items-center justify-center p-4 rounded-xl border-2 transition-all duration-200
                     ${activeClass}
                   `}
                 >
                   <Icon size={24} className={`mb-2 ${isActive ? (isDarkMode ? `text-${method.color}-400` : `text-${method.color}-600`) : (isDarkMode ? 'text-gray-500' : 'text-gray-400')}`} />
                   <span className="font-bold text-sm">{method.label}</span>
                 </button>
               );
             })}
          </div>

          {/* DYNAMIC CONTENT AREA */}
          <div className={`${isDarkMode ? 'bg-gray-800 border-gray-700' : 'bg-white border-gray-200'} rounded-2xl shadow-sm border p-6 min-h-[500px] flex flex-col`}>
            
            {/* --- CASH MODE --- */}
            {activeMethod === 'cash' && (
              <>
                {/* Currency Tabs */}
                <div className={`${isDarkMode ? 'bg-gray-700/50 border-gray-600' : 'bg-gray-50 border-gray-200'} p-1 rounded-xl border overflow-x-auto mb-6`}>
                  <div className="flex gap-1 min-w-max">
                    {Object.values(currencies).map((curr) => {
                      const hasAmount = curr.code === 'THB' 
                        ? thbAmount > 0 
                        : getForeignTotal(curr.code, foreignCounts[curr.code]) > 0;

                      return (
                        <button
                          key={curr.code}
                          onClick={() => setSelectedCurrency(curr.code)}
                          className={`relative flex items-center justify-center gap-3 px-6 py-3 rounded-lg transition-all duration-200 min-w-[120px] ${
                            selectedCurrency === curr.code
                              ? (isDarkMode ? 'bg-gray-600 text-white shadow-sm font-bold' : 'bg-white text-blue-600 shadow-sm font-bold')
                              : (isDarkMode ? 'hover:bg-gray-600 text-gray-400 font-medium' : 'hover:bg-gray-100 text-gray-500 font-medium')
                          }`}
                        >
                          <img 
                            src={`https://flagcdn.com/28x21/${curr.flagCode}.png`}
                            alt={curr.name}
                            className="h-5 w-auto object-cover rounded shadow-sm"
                          />
                          <span>{curr.code}</span>
                          
                          {/* Active Dot */}
                          {hasAmount && (
                            <span className="absolute top-2 right-2 w-2 h-2 rounded-full bg-green-500"></span>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </div>

                {/* Cash Headers */}
                <div className="flex justify-between items-center mb-6">
                  <h3 className={`text-lg font-bold ${isDarkMode ? 'text-gray-100' : 'text-gray-800'} flex items-center gap-2`}>
                    {selectedCurrency === 'THB' ? (
                      <><Calculator size={24} className="text-blue-600"/> Enter THB Amount</>
                    ) : (
                      <><Banknote size={24} className="text-blue-600"/> Count {selectedCurrency} Notes</>
                    )}
                  </h3>
                  
                  <div className="flex gap-2">
                    {/* Only show Clear Current if there is something to clear */}
                    {((selectedCurrency === 'THB' && thbAmount > 0) || (selectedCurrency !== 'THB' && getForeignTotal(selectedCurrency, foreignCounts[selectedCurrency]) > 0)) && (
                        <button onClick={handleClearCurrent} className={`flex items-center gap-1 px-3 py-1.5 text-xs font-medium rounded ${isDarkMode ? 'text-gray-300 bg-gray-700 hover:bg-gray-600' : 'text-gray-500 bg-gray-100 hover:bg-gray-200'}`}>
                          Clear {selectedCurrency}
                        </button>
                    )}
                    <button onClick={handleClearAll} className={`flex items-center gap-1 px-4 py-2 text-sm font-medium rounded-lg ${isDarkMode ? 'text-red-400 bg-red-900/20 hover:bg-red-900/30' : 'text-red-600 bg-red-50 hover:bg-red-100'}`}>
                      <Trash2 size={16} /> Reset All
                    </button>
                  </div>
                </div>

                {/* Cash Content */}
                {selectedCurrency === 'THB' ? (
                  // THB NUMPAD
                  <div className="flex-1 flex flex-col items-center justify-center max-w-md mx-auto w-full">
                    <div className={`w-full mb-6 border-2 rounded-2xl p-6 flex justify-end items-center h-24 ${isDarkMode ? 'bg-gray-700 border-gray-600' : 'bg-gray-50 border-gray-200'}`}>
                      <span className={`${isDarkMode ? 'text-gray-500' : 'text-gray-400'} text-2xl mr-2`}>฿</span>
                      <span className={`text-5xl font-mono font-bold tracking-wider ${isDarkMode ? 'text-white' : 'text-gray-800'}`}>
                        {thbAmount === 0 ? <span className={`${isDarkMode ? 'text-gray-600' : 'text-gray-300'}`}>0</span> : thbAmount.toLocaleString()}
                      </span>
                    </div>
                    <div className="grid grid-cols-3 gap-4 w-full">
                      {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((num) => (
                        <button
                          key={num}
                          onClick={() => handleTHBNumpad(num)}
                          disabled={isPaymentComplete}
                          className={`h-20 rounded-xl border shadow-sm text-3xl font-bold transition-all
                            ${isDarkMode 
                              ? 'bg-gray-700 border-gray-600 text-white hover:bg-gray-600 active:bg-gray-800' 
                              : 'bg-white border-gray-200 text-gray-700 hover:bg-blue-50 active:bg-blue-100 hover:border-blue-200'}`}
                        >
                          {num}
                        </button>
                      ))}
                      <button 
                        onClick={() => handleTHBNumpad('C')}
                        disabled={isPaymentComplete}
                        className={`h-20 rounded-xl border text-xl font-bold transition-all ${isDarkMode ? 'bg-red-900/20 border-red-900/30 text-red-400 hover:bg-red-900/40' : 'bg-red-50 border-red-100 text-red-600 hover:bg-red-100'}`}
                      >
                        C
                      </button>
                      <button 
                        onClick={() => handleTHBNumpad(0)}
                        disabled={isPaymentComplete}
                        className={`h-20 rounded-xl border shadow-sm text-3xl font-bold transition-all ${isDarkMode ? 'bg-gray-700 border-gray-600 text-white hover:bg-gray-600' : 'bg-white border-gray-200 text-gray-700 hover:bg-blue-50'}`}
                      >
                        0
                      </button>
                      <button 
                        onClick={() => handleTHBNumpad('BS')}
                        disabled={isPaymentComplete}
                        className={`h-20 rounded-xl border text-xl font-bold transition-all ${isDarkMode ? 'bg-gray-700 border-gray-600 text-gray-300 hover:bg-gray-600' : 'bg-gray-50 border-gray-200 text-gray-600 hover:bg-gray-100'}`}
                      >
                        ⌫
                      </button>
                    </div>
                    {/* Presets */}
                    <div className="grid grid-cols-4 gap-2 w-full mt-4">
                      {[100, 500, 1000, remainingDue > 0 ? remainingDue : billAmount].map((preset) => (
                        <button
                          key={preset}
                          onClick={() => setThbAmount(preset)}
                          disabled={isPaymentComplete}
                          className={`py-2 rounded-lg text-sm font-medium transition-colors ${isDarkMode ? 'bg-gray-700 text-gray-300 hover:bg-gray-600' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}
                        >
                          ฿{preset.toLocaleString()}
                        </button>
                      ))}
                    </div>
                  </div>
                ) : (
                  // FOREIGN COUNTER
                  <div className="flex flex-col h-full">
                    <div className={`flex justify-between items-end mb-6 p-4 rounded-xl border ${isDarkMode ? 'bg-blue-900/20 border-blue-800' : 'bg-blue-50 border-blue-100'}`}>
                      <div className={`text-sm font-medium ${isDarkMode ? 'text-blue-300' : 'text-blue-800'}`}>Total {selectedCurrency} Entered</div>
                      <div className={`text-3xl font-bold ${isDarkMode ? 'text-blue-200' : 'text-blue-900'}`}>
                        {formatMoney(getForeignTotal(selectedCurrency, foreignCounts[selectedCurrency]), selectedCurrency)}
                      </div>
                    </div>
                    <div className="grid grid-cols-2 md:grid-cols-3 gap-4 w-full">
                      {currencies[selectedCurrency].denominations.map((denom) => {
                        const currentCount = foreignCounts[selectedCurrency]?.[denom] || 0;
                        return (
                          <div 
                            key={denom}
                            className={`
                              relative p-4 rounded-xl border-2 transition-all duration-200 flex flex-col
                              ${currentCount > 0 
                                ? 'border-blue-500 shadow-md ' + (isDarkMode ? 'bg-gray-700' : 'bg-white') 
                                : (isDarkMode ? 'border-gray-700 bg-gray-700 hover:border-gray-500' : 'border-gray-200 bg-white hover:border-blue-200')
                              }
                            `}
                          >
                            <div className="flex justify-between items-start mb-3">
                              <span className={`font-bold text-xl ${isDarkMode ? 'text-gray-100' : 'text-gray-800'}`}>
                                {denom < 10 && selectedCurrency !== 'JPY' ? (
                                  <span className="flex items-center gap-1"><Coins size={18} className="text-yellow-500"/>{denom}</span>
                                ) : (
                                  <span className="flex items-center gap-1"><Banknote size={18} className="text-green-600"/>{denom}</span>
                                )}
                              </span>
                              {currentCount > 0 && (
                                <span className="bg-blue-600 text-white text-xs font-bold px-2 py-1 rounded-full">
                                  {currencies[selectedCurrency].symbol}{(denom * currentCount).toLocaleString()}
                                </span>
                              )}
                            </div>
                            <div className="flex items-center gap-2 mt-auto">
                              <span className={`text-xs font-bold uppercase tracking-wide ${isDarkMode ? 'text-gray-500' : 'text-gray-400'}`}>Count</span>
                              <input
                                type="number"
                                min="0"
                                placeholder="0"
                                disabled={isPaymentComplete}
                                value={currentCount || ''}
                                onChange={(e) => handleCountChange(denom, e.target.value)}
                                className={`
                                  w-full p-2 text-right rounded-lg border font-mono text-lg font-bold outline-none focus:ring-2 focus:ring-blue-500
                                  ${currentCount > 0 
                                    ? (isDarkMode ? 'bg-blue-900/20 border-blue-700 text-blue-300' : 'bg-blue-50 border-blue-200 text-blue-700') 
                                    : (isDarkMode ? 'bg-gray-600 border-gray-500 text-gray-400' : 'bg-gray-50 border-gray-200 text-gray-400')
                                  }
                                `}
                              />
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
              </>
            )}

            {/* --- NON-CASH MODES (Card, PromptPay, AliPay) --- */}
            {activeMethod !== 'cash' && (
              <div className="flex flex-col items-center justify-center h-full max-w-md mx-auto py-8">
                
                {/* Visual Icon */}
                <div className={`
                  w-24 h-24 rounded-full flex items-center justify-center mb-6 animate-in zoom-in duration-300
                  ${activeMethod === 'card' 
                    ? (isDarkMode ? 'bg-indigo-900/30 text-indigo-400' : 'bg-indigo-100 text-indigo-600')
                    : activeMethod === 'promptpay' 
                      ? (isDarkMode ? 'bg-sky-900/30 text-sky-400' : 'bg-sky-100 text-sky-600')
                      : (isDarkMode ? 'bg-cyan-900/30 text-cyan-400' : 'bg-cyan-100 text-cyan-600')
                  }
                `}>
                  {activeMethod === 'card' && <CreditCard size={48} />}
                  {activeMethod === 'promptpay' && <QrCode size={48} />}
                  {activeMethod === 'alipay' && <Smartphone size={48} />}
                </div>

                <h2 className={`text-2xl font-bold mb-2 ${isDarkMode ? 'text-gray-100' : 'text-gray-800'}`}>
                  {paymentMethods.find(m => m.id === activeMethod)?.label}
                </h2>
                <p className={`${isDarkMode ? 'text-gray-400' : 'text-gray-500'} mb-8 text-center`}>
                  {activeMethod === 'card' && 'Enter the amount to charge to the card.'}
                  {activeMethod === 'promptpay' && 'Enter amount to generate PromptPay QR.'}
                  {activeMethod === 'alipay' && 'Scan customer QR or generate payment code.'}
                </p>

                {/* Amount Input */}
                <div className="w-full mb-6">
                  <label className={`block text-sm font-bold mb-2 ${isDarkMode ? 'text-gray-300' : 'text-gray-700'}`}>Amount to Charge (THB)</label>
                  <div className="relative">
                    <span className={`absolute left-4 top-1/2 -translate-y-1/2 text-lg ${isDarkMode ? 'text-gray-500' : 'text-gray-400'}`}>฿</span>
                    <input 
                      type="number"
                      value={digitalAmountInput}
                      onChange={(e) => setDigitalAmountInput(e.target.value)}
                      placeholder="0.00"
                      className={`w-full pl-10 pr-4 py-4 text-3xl font-bold rounded-xl border-2 transition-all outline-none 
                        ${isDarkMode 
                          ? 'bg-gray-700 border-gray-600 text-white focus:border-blue-500 focus:bg-gray-600' 
                          : 'bg-gray-50 border-gray-200 text-gray-900 focus:border-blue-500 focus:bg-white'}
                      `}
                    />
                  </div>
                  {remainingDue > 0 && (
                     <div className="flex justify-end mt-2">
                       <button 
                         onClick={() => setDigitalAmountInput(remainingDue.toString())}
                         className={`text-sm font-bold hover:underline ${isDarkMode ? 'text-blue-400 hover:text-blue-300' : 'text-blue-600 hover:text-blue-800'}`}
                       >
                         Pay Remaining: ฿{remainingDue.toLocaleString()}
                       </button>
                     </div>
                  )}
                </div>

                {/* Action Button */}
                <button 
                  onClick={handleAddDigitalPayment}
                  disabled={!digitalAmountInput || parseFloat(digitalAmountInput) <= 0}
                  className={`
                    w-full py-4 rounded-xl text-lg font-bold text-white shadow-lg transition-all
                    ${!digitalAmountInput || parseFloat(digitalAmountInput) <= 0
                      ? (isDarkMode ? 'bg-gray-700 text-gray-500 cursor-not-allowed' : 'bg-gray-300 cursor-not-allowed')
                      : activeMethod === 'card' ? 'bg-indigo-600 hover:bg-indigo-700 shadow-indigo-200'
                      : activeMethod === 'promptpay' ? 'bg-sky-600 hover:bg-sky-700 shadow-sky-200'
                      : 'bg-cyan-600 hover:bg-cyan-700 shadow-cyan-200'
                    }
                  `}
                >
                  {activeMethod === 'card' ? 'Confirm Card Charge' : 'Generate Payment'}
                </button>

              </div>
            )}

          </div>
        </div>
      </div>
    </div>
  );
};

export default POSPayment;