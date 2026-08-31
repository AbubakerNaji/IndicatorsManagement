import { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router";
import api from "../../services/api";
import type { ApiResponse } from "../../services/authService";

interface SearchResult {
  type: string;
  id: number;
  titleAr: string;
  subtitleAr: string | null;
  status: string | null;
}

interface SearchResults {
  items: SearchResult[];
  totalCount: number;
}

const TYPE_LABELS: Record<string, string> = {
  Indicator: "مؤشر", Entity: "جهة", Entry: "إدخال",
};

export default function GlobalSearch() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResult[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setIsOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  // Debounced search
  useEffect(() => {
    if (query.length < 2) { setResults([]); return; }
    const timer = setTimeout(async () => {
      setLoading(true);
      try {
        const res = await api.get<ApiResponse<SearchResults>>("/search", { params: { q: query, pageSize: 10 } });
        setResults(res.data.data?.items || []);
        setIsOpen(true);
      } finally {
        setLoading(false);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [query]);

  // Save recent searches
  const saveRecent = (q: string) => {
    const recent = JSON.parse(localStorage.getItem("recentSearches") || "[]") as string[];
    const updated = [q, ...recent.filter(r => r !== q)].slice(0, 5);
    localStorage.setItem("recentSearches", JSON.stringify(updated));
  };

  const handleSelect = (result: SearchResult) => {
    saveRecent(query);
    setIsOpen(false);
    setQuery("");
    if (result.type === "Indicator") navigate(`/indicators`);
    else if (result.type === "Entity") navigate(`/entities`);
    else if (result.type === "Entry") navigate(`/review`);
  };

  return (
    <div ref={ref} className="relative w-full max-w-md">
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        onFocus={() => query.length >= 2 && setIsOpen(true)}
        placeholder="بحث في المؤشرات والجهات والإدخالات..."
        className="w-full px-4 py-2 text-sm border border-gray-200 rounded-lg bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:text-white focus:outline-none focus:border-brand-500"
      />

      {isOpen && (results.length > 0 || loading) && (
        <div className="absolute top-full mt-1 left-0 right-0 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg z-50 max-h-80 overflow-y-auto">
          {loading ? (
            <p className="p-3 text-sm text-gray-400 text-center">جارٍ البحث...</p>
          ) : (
            results.map((r, i) => (
              <button key={`${r.type}-${r.id}-${i}`} onClick={() => handleSelect(r)}
                className="w-full text-right px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700 border-b border-gray-100 dark:border-gray-700 last:border-0">
                <div className="flex items-center gap-2">
                  <span className="text-xs px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-600 text-gray-500 dark:text-gray-300">
                    {TYPE_LABELS[r.type] || r.type}
                  </span>
                  <span className="text-sm text-gray-800 dark:text-white/90">{r.titleAr}</span>
                </div>
                {r.subtitleAr && <p className="text-xs text-gray-400 mt-0.5">{r.subtitleAr}</p>}
              </button>
            ))
          )}
        </div>
      )}
    </div>
  );
}
