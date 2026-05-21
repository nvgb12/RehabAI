/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        rehab: {
          50: '#effaf8',
          100: '#d8f2ef',
          200: '#b6e5df',
          300: '#85d1c8',
          400: '#4bb4aa',
          500: '#259890',
          600: '#1c7a75',
          700: '#1b625f',
          800: '#194f4d',
          900: '#184240',
        },
        care: {
          50: '#eef7ff',
          100: '#d9edff',
          200: '#bbe0ff',
          300: '#8bccff',
          400: '#54aeff',
          500: '#2d8df0',
          600: '#176fcd',
          700: '#1559a6',
          800: '#174d86',
          900: '#183f6c',
        },
      },
      boxShadow: {
        soft: '0 18px 45px rgba(21, 89, 166, 0.10)',
      },
    },
  },
  plugins: [],
}
