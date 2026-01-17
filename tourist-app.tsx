import React, { useState, useEffect, useRef } from 'react';
import { 
  MapPin, 
  Navigation, 
  MessageSquare, 
  ShieldAlert, 
  Clock, 
  Key, 
  Info, 
  ChevronRight, 
  Search,
  Settings,
  X,
  Send,
  Fuel,
  Camera,
  Heart,
  ExternalLink,
  ChevronLeft,
  Bike,
  Sun,
  CloudRain,
  Navigation2,
  Phone,
  Droplets,
  Zap
} from 'lucide-react';

const apiKey = ""; // Environment will provide key

const PhuketBikersApp = () => {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [showAssistant, setShowAssistant] = useState(false);
  const [messages, setMessages] = useState([
    { role: 'assistant', text: "Sawadee ka! Welcome to Phuket Bikers. I'm your virtual road captain. Ready for a ride to the Big Buddha or a sunset tour? 🏍️" }
  ]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const scrollRef = useRef(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages]);

  // Mock Data
  const rentalData = {
    vehicle: "Honda ADV 160",
    plate: "กข 1234 ภูเก็ต",
    timeLeft: "2d 04h",
    fuelLevel: 85,
    weather: { temp: "31°C", condition: "Sunny", icon: Sun, chanceOfRain: "10%" }
  };

  const spots = [
    { name: "Big Buddha", type: "Must Visit", distance: "4.2km", icon: "🕉️", color: "bg-purple-50", text: "text-purple-600" },
    { name: "Promthep Cape", type: "Best Sunset", distance: "12km", icon: "🌅", color: "bg-orange-50", text: "text-orange-600" },
    { name: "Old Phuket Town", type: "Food & Photo", distance: "8.5km", icon: "🏘️", color: "bg-blue-50", text: "text-blue-600" },
    { name: "Yanui Beach", type: "Hidden Gem", distance: "13.2km", icon: "🏖️", color: "bg-teal-50", text: "text-teal-600" }
  ];

  const handleSendMessage = async () => {
    if (!input.trim() || isLoading) return;
    const userMessage = input;
    setInput("");
    setMessages(prev => [...prev, { role: 'user', text: userMessage }]);
    setIsLoading(true);

    try {
      const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-09-2025:generateContent?key=${apiKey}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contents: [{ parts: [{ text: userMessage }] }],
          systemInstruction: { 
            parts: [{ text: "You are 'Phuket Biker Pal', a cool, friendly road captain for Phuket Bikers rental. Help tourists with riding tips, hidden spots, and safety. Remind them to wear helmets and drive on the left. Use emojis and Thai greetings. Be concise." }] 
          }
        })
      });
      const data = await response.json();
      const botText = data.candidates?.[0]?.content?.parts?.[0]?.text || "Signal's a bit weak near the mountains! Try again?";
      setMessages(prev => [...prev, { role: 'assistant', text: botText }]);
    } catch (error) {
      setMessages(prev => [...prev, { role: 'assistant', text: "Lost connection! Check your SIM card." }]);
    } finally {
      setIsLoading(false);
    }
  };

  const RenderDashboard = () => (
    <div className="space-y-6 animate-in fade-in duration-700">
      {/* Dynamic Header: Weather & Greeting */}
      <div className="flex justify-between items-end">
        <div>
          <h2 className="text-2xl font-black text-slate-800">Hello, Rider!</h2>
          <p className="text-slate-500 text-sm">Perfect day for a ride 🌴</p>
        </div>
        <div className="bg-white px-3 py-2 rounded-2xl shadow-sm border border-slate-100 flex items-center gap-3">
          <rentalData.weather.icon className="text-amber-500" size={20} />
          <div>
            <p className="font-bold text-sm leading-none">{rentalData.weather.temp}</p>
            <p className="text-[10px] text-slate-400">Rain: {rentalData.weather.chanceOfRain}</p>
          </div>
        </div>
      </div>

      {/* Active Rental Card - Refined */}
      <div className="bg-slate-900 rounded-[2.5rem] p-6 text-white shadow-2xl relative overflow-hidden group">
        <div className="absolute top-0 right-0 w-40 h-40 bg-teal-500/20 rounded-full -mr-20 -mt-20 blur-3xl group-hover:bg-teal-500/30 transition-colors" />
        <div className="relative z-10">
          <div className="flex justify-between items-center mb-6">
            <div className="flex items-center gap-3">
              <div className="bg-teal-500 p-2.5 rounded-2xl">
                <Bike size={20} />
              </div>
              <div>
                <h3 className="font-bold text-lg">{rentalData.vehicle}</h3>
                <p className="text-[10px] text-slate-400 uppercase tracking-widest">{rentalData.plate}</p>
              </div>
            </div>
            <div className="text-right">
              <p className="text-[10px] text-slate-400 uppercase font-bold">Return In</p>
              <p className="text-teal-400 font-black text-xl">{rentalData.timeLeft}</p>
            </div>
          </div>
          
          <div className="flex gap-2">
            <div className="flex-1 bg-white/5 rounded-2xl p-3 border border-white/5">
                <div className="flex items-center gap-2 text-slate-400 mb-1">
                    <Droplets size={12} className="text-teal-400" />
                    <span className="text-[10px] font-bold uppercase">Fuel</span>
                </div>
                <div className="flex items-end gap-2">
                    <p className="font-bold text-lg">{rentalData.fuelLevel}%</p>
                    <div className="flex-1 h-1 bg-white/10 rounded-full mb-2">
                        <div className="h-full bg-teal-500 rounded-full" style={{ width: `${rentalData.fuelLevel}%` }} />
                    </div>
                </div>
            </div>
            <button className="bg-teal-500 hover:bg-teal-400 text-slate-900 font-bold px-6 rounded-2xl transition-all active:scale-95 flex items-center justify-center">
              Extend
            </button>
          </div>
        </div>
      </div>

      {/* Quick Actions Grid */}
      <div className="grid grid-cols-4 gap-3">
        {[
          { icon: ShieldAlert, label: "SOS", color: "bg-rose-50 text-rose-600" },
          { icon: Navigation2, label: "Map", color: "bg-indigo-50 text-indigo-600" },
          { icon: Phone, label: "Support", color: "bg-emerald-50 text-emerald-600" },
          { icon: Zap, label: "Tips", color: "bg-amber-50 text-amber-600", onClick: () => setShowAssistant(true) }
        ].map((item, i) => (
          <button key={i} onClick={item.onClick} className="flex flex-col items-center gap-2">
            <div className={`${item.color} w-full aspect-square rounded-[1.5rem] flex items-center justify-center shadow-sm hover:shadow-md transition-all active:scale-90`}>
              <item.icon size={24} />
            </div>
            <span className="text-[10px] font-bold text-slate-400 uppercase tracking-tight">{item.label}</span>
          </button>
        ))}
      </div>

      {/* Local Hack Section */}
      <div className="bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-100 rounded-3xl p-5 relative overflow-hidden">
        <div className="relative z-10 flex gap-4">
          <div className="bg-amber-400/20 p-3 rounded-2xl h-fit">
            <Zap className="text-amber-600" size={20} />
          </div>
          <div>
            <h4 className="font-black text-amber-900 text-sm uppercase">Pro Biker Hack</h4>
            <p className="text-xs text-amber-800/80 leading-relaxed mt-1">
                Avoid the main highway to Rawai between 4 PM - 6 PM. Take the coastal backroad for zero traffic and better views! 🌊
            </p>
          </div>
        </div>
      </div>

      {/* Adventure Feed */}
      <div>
        <div className="flex justify-between items-center mb-4">
          <h3 className="font-black text-slate-800 tracking-tight">Adventure Feed</h3>
          <button className="text-teal-600 text-xs font-bold uppercase tracking-wider">Nearby Only</button>
        </div>
        <div className="grid grid-cols-1 gap-4">
          {spots.map((spot, i) => (
            <div key={i} className="bg-white p-4 rounded-[2rem] border border-slate-100 shadow-sm flex items-center gap-4 group hover:border-teal-200 transition-all cursor-pointer">
              <div className={`${spot.color} ${spot.text} text-2xl w-14 h-14 flex items-center justify-center rounded-2xl shadow-inner group-hover:scale-110 transition-transform`}>
                {spot.icon}
              </div>
              <div className="flex-1">
                <div className="flex items-center gap-2">
                    <span className={`text-[9px] font-black uppercase tracking-widest px-2 py-0.5 rounded-full ${spot.color} ${spot.text}`}>
                        {spot.type}
                    </span>
                    <span className="text-[10px] text-slate-400">• {spot.distance}</span>
                </div>
                <h4 className="font-bold text-slate-800 text-base">{spot.name}</h4>
              </div>
              <div className="p-2 bg-slate-50 rounded-xl text-slate-300 group-hover:text-teal-500 group-hover:bg-teal-50 transition-colors">
                <Navigation2 size={18} />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );

  const RenderMap = () => (
    <div className="h-full flex flex-col gap-4 animate-in slide-in-from-right duration-500">
      <div className="relative flex-1 bg-slate-100 rounded-[2.5rem] overflow-hidden border border-slate-200 shadow-inner">
        <div className="absolute inset-0 bg-[#f8fafc]" style={{
          backgroundImage: `radial-gradient(#cbd5e1 0.8px, transparent 0.8px)`,
          backgroundSize: '30px 30px'
        }} />
        <div className="absolute inset-0 flex flex-col items-center justify-center text-center p-10">
            <div className="relative">
                <div className="absolute inset-0 bg-teal-500 blur-2xl opacity-20 animate-pulse" />
                <MapPin size={56} className="text-teal-600 relative animate-bounce" />
            </div>
            <h3 className="text-slate-800 font-black text-xl mt-4">Live Navigator</h3>
            <p className="text-slate-500 text-sm mt-2 max-w-[200px]">Optimizing routes for the best island views...</p>
        </div>
        
        {/* Floating Marker UI */}
        <div className="absolute top-1/4 left-1/3 p-2 bg-white rounded-2xl shadow-xl flex items-center gap-2 border border-slate-100 animate-in zoom-in duration-1000">
            <div className="w-8 h-8 bg-orange-100 rounded-xl flex items-center justify-center text-orange-600">🌅</div>
            <div className="pr-2">
                <p className="text-[10px] font-black leading-none">Sunset Point</p>
                <p className="text-[8px] text-slate-400">12 min away</p>
            </div>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <button className="bg-white p-4 rounded-3xl border border-slate-100 shadow-sm flex items-center gap-3 active:scale-95 transition-transform">
            <Fuel className="text-teal-500" size={20} />
            <div className="text-left">
                <p className="font-bold text-xs">Gas Stations</p>
                <p className="text-[10px] text-slate-400">Find nearby</p>
            </div>
        </button>
        <button className="bg-white p-4 rounded-3xl border border-slate-100 shadow-sm flex items-center gap-3 active:scale-95 transition-transform">
            <Camera className="text-rose-500" size={20} />
            <div className="text-left">
                <p className="font-bold text-xs">Photo Spots</p>
                <p className="text-[10px] text-slate-400">Insta-worthy</p>
            </div>
        </button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-slate-50 font-sans text-slate-900 flex justify-center selection:bg-teal-100">
      <div className="w-full max-w-md bg-white min-h-screen shadow-2xl flex flex-col relative overflow-hidden border-x border-slate-100">
        
        {/* Header Navigation */}
        <header className="px-6 pt-8 pb-4 flex justify-between items-center bg-white/80 backdrop-blur-md sticky top-0 z-30">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-teal-500 rounded-[1.2rem] flex items-center justify-center text-slate-900 shadow-lg shadow-teal-500/20 rotate-3">
              <Bike size={24} strokeWidth={3} />
            </div>
            <div>
              <h1 className="text-xl font-black tracking-tight text-slate-900 leading-none">Phuket Bikers</h1>
              <div className="flex items-center gap-1 mt-1">
                <div className="w-1.5 h-1.5 bg-green-500 rounded-full animate-pulse" />
                <p className="text-[9px] text-slate-400 font-bold uppercase tracking-widest">Connected</p>
              </div>
            </div>
          </div>
          <div className="flex gap-2">
            <button className="p-2.5 bg-slate-50 rounded-2xl text-slate-400 hover:text-slate-600 transition-colors">
                <Search size={20} />
            </button>
            <button className="p-2.5 bg-slate-50 rounded-2xl text-slate-400 hover:text-slate-600 transition-colors">
                <Settings size={20} />
            </button>
          </div>
        </header>

        {/* Dynamic Content */}
        <main className="flex-1 px-6 pb-28 overflow-y-auto pt-2">
          {activeTab === 'dashboard' && <RenderDashboard />}
          {activeTab === 'map' && <RenderMap />}
          {activeTab === 'docs' && (
             <div className="animate-in slide-in-from-left duration-500 space-y-6">
                <div>
                    <h2 className="text-2xl font-black text-slate-800">Safety & Rules</h2>
                    <p className="text-slate-500 text-sm">Essential info for a smooth ride.</p>
                </div>
                
                <div className="grid grid-cols-1 gap-4">
                    {[
                        { title: "Left Side Traffic", desc: "Always stay on the left. Thai traffic can be unpredictable.", icon: "⬅️" },
                        { title: "Helmet Policy", desc: "Mandatory for driver & pillion. Police check often.", icon: "🪖" },
                        { title: "Fuel Logic", desc: "Look for '91' or '95' gas. Red bottles are roadside spares.", icon: "⛽" }
                    ].map((rule, i) => (
                        <div key={i} className="p-5 bg-slate-50 rounded-3xl border border-slate-100 flex gap-4">
                            <span className="text-3xl">{rule.icon}</span>
                            <div>
                                <h4 className="font-bold text-slate-800">{rule.title}</h4>
                                <p className="text-xs text-slate-500 mt-1 leading-relaxed">{rule.desc}</p>
                            </div>
                        </div>
                    ))}
                </div>

                <div className="mt-8">
                    <h3 className="font-black text-slate-800 text-sm uppercase tracking-widest mb-4">Digital Documents</h3>
                    <div className="space-y-3">
                        {["Rental Contract", "Insurance Policy", "Passport Verified"].map((doc, i) => (
                            <div key={i} className="flex items-center justify-between p-4 bg-white rounded-2xl border border-slate-100">
                                <div className="flex items-center gap-3">
                                    <div className="w-8 h-8 bg-teal-50 text-teal-600 rounded-lg flex items-center justify-center">
                                        <Info size={16} />
                                    </div>
                                    <span className="text-sm font-bold text-slate-700">{doc}</span>
                                </div>
                                <ExternalLink size={16} className="text-slate-300" />
                            </div>
                        ))}
                    </div>
                </div>
             </div>
          )}
        </main>

        {/* AI Assistant Overlay */}
        {showAssistant && (
          <div className="absolute inset-0 z-50 bg-white flex flex-col animate-in slide-in-from-bottom duration-400">
            <div className="px-6 py-6 flex items-center justify-between border-b border-slate-50 bg-slate-900 text-white">
              <div className="flex items-center gap-4">
                <div className="relative">
                    <div className="w-12 h-12 bg-teal-500 rounded-[1.2rem] flex items-center justify-center shadow-xl shadow-teal-500/20 rotate-6">
                        <Bike size={24} />
                    </div>
                    <div className="absolute -bottom-1 -right-1 w-4 h-4 bg-green-500 border-4 border-slate-900 rounded-full" />
                </div>
                <div>
                  <h3 className="font-black text-lg leading-none">Biker Pal</h3>
                  <span className="text-[10px] text-teal-400 font-bold uppercase tracking-widest">Expert Road Captain</span>
                </div>
              </div>
              <button onClick={() => setShowAssistant(false)} className="p-3 bg-white/10 rounded-2xl hover:bg-white/20 transition-colors">
                <X size={20} />
              </button>
            </div>
            
            <div ref={scrollRef} className="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-50">
              {messages.map((msg, i) => (
                <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                  <div className={`max-w-[85%] px-5 py-4 rounded-3xl text-sm leading-relaxed shadow-sm ${
                    msg.role === 'user' 
                    ? 'bg-slate-900 text-white rounded-tr-none' 
                    : 'bg-white text-slate-800 rounded-tl-none border border-slate-200/50'
                  }`}>
                    {msg.text}
                  </div>
                </div>
              ))}
              {isLoading && (
                <div className="flex justify-start">
                  <div className="bg-white px-5 py-4 rounded-3xl rounded-tl-none border border-slate-200/50 flex gap-1.5 items-center">
                    <div className="w-1.5 h-1.5 bg-teal-500 rounded-full animate-bounce" />
                    <div className="w-1.5 h-1.5 bg-teal-500 rounded-full animate-bounce [animation-delay:-0.15s]" />
                    <div className="w-1.5 h-1.5 bg-teal-500 rounded-full animate-bounce [animation-delay:-0.3s]" />
                  </div>
                </div>
              )}
            </div>

            <div className="p-6 bg-white border-t border-slate-100">
              <div className="flex gap-2 p-1.5 bg-slate-100 rounded-[2rem]">
                <input 
                  type="text" 
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSendMessage()}
                  placeholder="Ask about traffic, food, or viewpoints..."
                  className="flex-1 bg-transparent border-none focus:ring-0 px-4 py-3 text-sm font-medium"
                />
                <button 
                  onClick={handleSendMessage}
                  disabled={isLoading}
                  className="bg-slate-900 text-white p-3 rounded-full disabled:opacity-50 active:scale-95 transition-all shadow-lg shadow-slate-900/10"
                >
                  <Send size={20} />
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Ultra-Modern Bottom Navigation */}
        <nav className="fixed bottom-6 left-1/2 -translate-x-1/2 w-[90%] max-w-sm h-20 bg-slate-900/90 backdrop-blur-2xl rounded-[2.5rem] border border-white/10 flex items-center justify-around px-2 z-40 shadow-2xl">
          {[
            { id: 'dashboard', icon: Bike, label: 'Ride' },
            { id: 'map', icon: Navigation2, label: 'Routes' },
            { id: 'docs', icon: ShieldAlert, label: 'Safety' },
          ].map((item) => (
            <button
              key={item.id}
              onClick={() => setActiveTab(item.id)}
              className={`relative flex flex-col items-center gap-1 transition-all duration-300 ${
                activeTab === item.id ? 'text-teal-400 -translate-y-1' : 'text-slate-500'
              }`}
            >
              <item.icon size={22} strokeWidth={activeTab === item.id ? 2.5 : 2} />
              <span className={`text-[9px] font-black uppercase tracking-widest ${activeTab === item.id ? 'opacity-100' : 'opacity-40'}`}>
                {item.label}
              </span>
              {activeTab === item.id && (
                <div className="absolute -bottom-2 w-1 h-1 bg-teal-400 rounded-full shadow-[0_0_10px_#2dd4bf]" />
              )}
            </button>
          ))}
          
          <button 
            onClick={() => setShowAssistant(true)}
            className="group relative"
          >
            <div className="bg-teal-500 text-slate-900 p-4 rounded-[1.8rem] -mt-12 shadow-xl shadow-teal-500/30 border-4 border-white active:scale-90 transition-all group-hover:rotate-6">
              <MessageSquare size={24} strokeWidth={3} />
            </div>
            <span className="text-[9px] font-black uppercase tracking-widest text-slate-400 absolute left-1/2 -translate-x-1/2 top-11">Talk</span>
          </button>
        </nav>

        {/* Notch Guard */}
        <div className="absolute top-2 left-1/2 -translate-x-1/2 w-36 h-7 bg-slate-900 rounded-full z-50 pointer-events-none opacity-[0.03] md:opacity-0" />
      </div>

      <style>{`
        @keyframes fade-in { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
        .scrollbar-hide::-webkit-scrollbar { display: none; }
        .scrollbar-hide { -ms-overflow-style: none; scrollbar-width: none; }
      `}</style>
    </div>
  );
};

export default function App() {
  return <PhuketBikersApp />;
}