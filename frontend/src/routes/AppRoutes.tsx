import { Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from '../layouts/MainLayout'
import { AdminDashboardPage } from '../pages/AdminDashboardPage'
import { AdminDoctorsPage } from '../pages/AdminDoctorsPage'
import { AdminOrdersPage } from '../pages/AdminOrdersPage'
import { AdminProductsPage } from '../pages/AdminProductsPage'
import { AdminReportsPage } from '../pages/AdminReportsPage'
import { AdminServicesPage } from '../pages/AdminServicesPage'
import { DoctorDetailPage } from '../pages/DoctorDetailPage'
import { DoctorListPage } from '../pages/DoctorListPage'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { PatientAppointmentsPage } from '../pages/PatientAppointmentsPage'
import { PatientDashboardPage } from '../pages/PatientDashboardPage'
import { PatientOrdersPage } from '../pages/PatientOrdersPage'
import { PatientProfilePage } from '../pages/PatientProfilePage'
import { PatientSubscriptionPage } from '../pages/PatientSubscriptionPage'
import { ProductDetailPage } from '../pages/ProductDetailPage'
import { ProductListPage } from '../pages/ProductListPage'
import { RegisterPage } from '../pages/RegisterPage'
import { ProtectedRoute } from './ProtectedRoute'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/products" element={<ProductListPage />} />
        <Route path="/products/:productId" element={<ProductDetailPage />} />
        <Route path="/doctors" element={<DoctorListPage />} />
        <Route path="/doctors/:doctorProfileId" element={<DoctorDetailPage />} />
        <Route
          path="/patient/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Patient']}>
              <PatientDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/patient/profile"
          element={
            <ProtectedRoute allowedRoles={['Patient']}>
              <PatientProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/patient/appointments"
          element={
            <ProtectedRoute allowedRoles={['Patient']}>
              <PatientAppointmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/patient/orders"
          element={
            <ProtectedRoute allowedRoles={['Patient']}>
              <PatientOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/patient/subscription"
          element={
            <ProtectedRoute allowedRoles={['Patient']}>
              <PatientSubscriptionPage />
            </ProtectedRoute>
          }
        />
        <Route path="/profile" element={<Navigate to="/patient/profile" replace />} />
        <Route
          path="/appointments"
          element={<Navigate to="/patient/appointments" replace />}
        />
        <Route path="/my-orders" element={<Navigate to="/patient/orders" replace />} />
        <Route
          path="/subscription"
          element={<Navigate to="/patient/subscription" replace />}
        />
        <Route
          path="/admin/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
        <Route
          path="/admin/products"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminProductsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/orders"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminOrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/reports"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminReportsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/services"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminServicesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/doctors"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminDoctorsPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
