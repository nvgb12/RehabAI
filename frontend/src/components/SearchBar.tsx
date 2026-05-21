import { Search } from 'lucide-react'

interface SearchBarProps {
  value: string
  placeholder: string
  onChange: (value: string) => void
}

export function SearchBar({ value, placeholder, onChange }: SearchBarProps) {
  return (
    <label className="relative block">
      <Search
        className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
        aria-hidden="true"
      />
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="field-input pl-12"
        type="search"
      />
    </label>
  )
}
