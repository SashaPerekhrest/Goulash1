type EmptyStateProps = {
  title: string;
  text: string;
};

export function EmptyState({ title, text }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <h2>{title}</h2>
      <p>{text}</p>
    </div>
  );
}
