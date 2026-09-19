import { Box, Stack, Typography } from "@mui/material";

interface AdminPagePlaceholderProps {
  title: string;
  caption: string;
}

export function AdminPagePlaceholder({ title, caption }: AdminPagePlaceholderProps) {
  return (
    <Stack spacing={3}>
      <Box>
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 1 }} variant="body1">
          {caption}
        </Typography>
      </Box>

      <Box
        sx={{
          alignItems: "center",
          border: 1,
          borderColor: "divider",
          borderRadius: 1,
          display: "flex",
          minHeight: 220,
          p: 3
        }}
      >
        <Typography color="text.secondary">Раздел подключен к защищенной зоне.</Typography>
      </Box>
    </Stack>
  );
}
