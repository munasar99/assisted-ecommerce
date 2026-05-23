import { useQuery } from "@tanstack/react-query";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from "recharts";
import { analyticsApi } from "../../api/endpoints";
import Loader from "../../components/Loader";

const COLORS = ["#0d9488", "#14b8a6", "#5eead4", "#99f6e4", "#ccfbf1"];

export default function AnalyticsPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["analytics"],
    queryFn: analyticsApi.dashboard,
    staleTime: 2 * 60 * 1000,
    gcTime: 5 * 60 * 1000,
  });

  if (isLoading) return <Loader />;
  if (error) return <p className="text-red-600">{error.message}</p>;

  return (
    <div>
      <h1 className="text-2xl font-bold">Analytics</h1>
      <div className="mt-6 grid gap-8 lg:grid-cols-2">
        <div className="rounded-xl border bg-white p-4 h-72">
          <h3 className="mb-2 font-semibold">Orders by status</h3>
          <ResponsiveContainer width="100%" height="90%">
            <BarChart data={data.ordersByStatus || []}>
              <XAxis dataKey="status" tick={{ fontSize: 10 }} />
              <YAxis />
              <Tooltip />
              <Bar dataKey="count" fill="#0d9488" />
            </BarChart>
          </ResponsiveContainer>
        </div>
        <div className="rounded-xl border bg-white p-4 h-72">
          <h3 className="mb-2 font-semibold">Top districts</h3>
          <ResponsiveContainer width="100%" height="90%">
            <PieChart>
              <Pie data={data.ordersByDistrict || []} dataKey="count" nameKey="districtId" cx="50%" cy="50%" outerRadius={80}>
                {(data.ordersByDistrict || []).map((_, i) => (
                  <Cell key={i} fill={COLORS[i % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}
